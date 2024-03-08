using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;
using System.Globalization;
using System.Linq;

namespace Medal.PurchaseOrder
{
    public class PurchaseOrderConvertToFixedWidthFormat
    {
        private readonly ILogger<PurchaseOrderConvertToFixedWidthFormat> _logger;

        public PurchaseOrderConvertToFixedWidthFormat(ILogger<PurchaseOrderConvertToFixedWidthFormat> logger)
        {
            _logger = logger;
        }

        [Function("PurchaseOrderConvertToFixedWidthFormat")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            var body = req.Body.ToString();
            if (string.IsNullOrEmpty(body))
            {
                return new BadRequestObjectResult("Invalid request body.");
            }

            var returnValue = ProcessOrder(body);
            return new OkObjectResult(returnValue);
        }

        public string ProcessOrder(string input)
        {
            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(input);

            var order = new Order();
            order.OrderLines = new List<OrderLine>();

            var tables = htmlDoc.DocumentNode.SelectNodes("//table");
            var orderTable = tables[0];
            var orderLineTable = tables[1];

            // Parse order data
            var orderData = orderTable.SelectNodes("//tr")[1].SelectNodes("//td").Select(node => node.InnerText).ToList();
            order.Customer = orderData[0];
            order.PurchaseOrderNo = orderData[1];
            order.UNH_ID = orderData[4];

            DateTime orderDate;
            if (DateTime.TryParseExact(orderData[2], "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out orderDate))
            {
                order.OrderDate = orderDate;
            }
            else
            {
                order.OrderDate = DateTime.MinValue;
            }

            DateTime promisedDate;
            if (DateTime.TryParseExact(orderData[3], "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out promisedDate))
            {
                order.PromisedDate = promisedDate;
            }
            else
            {
                order.PromisedDate = DateTime.MinValue;
            }

            // Parse order line data
            var orderLineRows = orderLineTable.SelectNodes("//tr").Skip(1);
            int itemNumber = 1;
            foreach (var row in orderLineRows)
            {
                var orderLineData = row.SelectNodes("//td").Select(orderLineNode => orderLineNode.InnerText).ToList();
                var orderLine = new OrderLine
                {
                    ItemNumber = itemNumber.ToString().PadLeft(4, '0'),
                    ProductCode = orderLineData[0],
                    Description = orderLineData[3]
                };

                int quantity;
                if (int.TryParse(orderLineData[1], out quantity))
                {
                    orderLine.Quantity = quantity;
                }
                else
                {
                    orderLine.Quantity = 0;
                }

                decimal price;
                if (decimal.TryParse(orderLineData[2], out price))
                {
                    orderLine.Price = price;
                }
                else
                {
                    orderLine.Price = 0m;
                }

                order.OrderLines.Add(orderLine);
                itemNumber++;
            }

            return FormatOrder(order);
        }

        private string FormatOrder(Order order)
        {
            string result = "";

            result += "UNH  " 
                + order.UNH_ID?.PadRight(6, ' ');
            result += "\nCLO  " 
                + order.Customer?.PadRight(7, ' ') 
                + order.PurchaseOrderNo?.PadRight(24, ' ') 
                + order.OrderDate.ToString("yyyyMMdd") + " " 
                + order.PromisedDate.ToString("yyyyMMdd");
                
            foreach (var line in from line in order.OrderLines
                                 where line != null
                                 select line)
            {
                result += "\nOLD  "
                                    + line.ItemNumber
                                    + " "
                                    + line.ProductCode?.PadRight(15, ' ')
                                    + " "
                                    + line.Quantity.ToString().PadLeft(10, '0')
                                    + "."
                                    + line.Price.ToString("F3").PadLeft(8, '0');
                result += "\nOLDA "
                                    + line.Description?.PadRight(49, ' ');
            }

            return result;
        }

    }

    public class Order
    {
        public string? UNH_ID { get; set; }
        public string? Customer { get; set; }
        public string? PurchaseOrderNo { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime PromisedDate { get; set; }
        public List<OrderLine>? OrderLines { get; set; }
    }

    public class OrderLine
    {
        internal string? ItemNumber;
        public string? ProductCode { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
    }
}
