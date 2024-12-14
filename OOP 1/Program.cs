using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;

namespace ProductInventorySystem
{
    class Program
    {
        static void Main(string[] args)
        {
            MainMenu menu = new MainMenu();
            menu.Run();
        }
    }

    class Product
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public override string ToString()
        {
            return $"{ID},{Name},{Category},{Quantity},{Price}";
        }

        public static Product FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');

            if (values.Length != 5) return null;

            try
            {
                return new Product
                {
                    ID = values[0],
                    Name = values[1],
                    Category = values[2],
                    Quantity = int.Parse(values[3]),
                    Price = decimal.Parse(values[4])
                };
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Error parsing line: {csvLine}. Details: {ex.Message}");
                return null;
            }
        }
    }

    class MainMenu
    {
        private const string FilePath = "data/inventory.csv";
        private const string ReportsDir = "reports";
        private const string ReceiptsDir = "receipts";
        private const string ManagerPassword = "admin";
        private List<Product> products = new List<Product>();
        private List<Product> cart = new List<Product>();

        public MainMenu()
        {
            SetupEnvironment();
            LoadProductsFromCsv();
        }

        public void Run()
        {
            Console.Clear();
            Console.WriteLine("Welcome to the Inventory System!");
            Console.WriteLine("1. Staff Mode");
            Console.WriteLine("2. Manager Mode");
            Console.Write("Select mode (1 or 2): ");
            string modeSelection = Console.ReadLine();

            if (modeSelection == "1")
            {
                StaffMode();
            }
            else if (modeSelection == "2")
            {
                if (AuthenticateManager())
                {
                    ManagerMode();
                }
                else
                {
                    Console.WriteLine("Authentication failed. Returning to main menu.");
                    Pause();
                    Run();
                }
            }
            else
            {
                Console.WriteLine("Invalid selection. Exiting program.");
            }
        }

        private bool AuthenticateManager()
        {
            Console.Write("Enter Manager Password: ");
            string password = Console.ReadLine();
            return password == ManagerPassword;
        }

        private void StaffMode()
        {
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("Staff Menu:");
                Console.WriteLine("1. Place Order");
                Console.WriteLine("2. View Inventory");
                Console.WriteLine("3. Search Product");
                Console.WriteLine("4. Go Back to Mode Selection");
                Console.WriteLine("5. Exit");
                Console.Write("Select an option: ");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        PlaceOrder();
                        break;
                    case "2":
                        ViewInventory();
                        break;
                    case "3":
                        SearchProduct();
                        break;
                    case "4":
                        Console.WriteLine("Returning to mode selection...");
                        exit = true;
                        Run();
                        break;
                    case "5":
                        exit = true;
                        Console.WriteLine("Exiting program. Goodbye!");
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        Pause();
                        break;
                }
            }
        }

        private void ManagerMode()
        {
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("Manager Menu:");
                Console.WriteLine("1. Add Product");
                Console.WriteLine("2. Update Product");
                Console.WriteLine("3. Delete Product");
                Console.WriteLine("4. View Inventory");
                Console.WriteLine("5. Search Product");
                Console.WriteLine("6. Save Changes");
                Console.WriteLine("7. Go Back to Mode Selection");
                Console.WriteLine("8. Exit");
                Console.Write("Select an option: ");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        AddProduct();
                        break;
                    case "2":
                        UpdateProduct();
                        break;
                    case "3":
                        DeleteProduct();
                        break;
                    case "4":
                        ViewInventory();
                        break;
                    case "5":
                        SearchProduct();
                        break;
                    case "6":
                        SaveProductsToCsv();
                        Console.WriteLine("Changes saved successfully.");
                        Pause();
                        break;
                    case "7":
                        Console.WriteLine("Returning to mode selection...");
                        exit = true;
                        Run();
                        break;
                    case "8":
                        exit = true;
                        Console.WriteLine("Exiting program. Goodbye!");
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        Pause();
                        break;
                }
            }
        }

        private void SetupEnvironment()
        {
            try
            {
                string dataDir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                    Console.WriteLine($"Directory '{dataDir}' created.");
                }
                if (!Directory.Exists(ReportsDir))
                {
                    Directory.CreateDirectory(ReportsDir);
                    Console.WriteLine($"Directory '{ReportsDir}' created.");
                }
                if (!Directory.Exists(ReceiptsDir))
                {
                    Directory.CreateDirectory(ReceiptsDir);
                    Console.WriteLine($"Directory '{ReceiptsDir}' created.");
                }
                if (!File.Exists(FilePath))
                {
                    File.WriteAllText(FilePath, "ID,Name,Category,Quantity,Price");
                    Console.WriteLine($"File '{FilePath}' created with headers.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up environment: {ex.Message}");
            }
        }

        private void PlaceOrder()
        {
            bool checkout = false;

            while (!checkout)
            {
                Console.Clear();

                // Display available categories in a table format
                Console.WriteLine("Available Categories:");
                var categories = products.Select(p => p.Category).Distinct().ToList();

                if (categories.Any())
                {
                    int categoryWidth = Math.Max(15, categories.Max(c => c.Length) + 2);
                    Console.WriteLine(new string('-', categoryWidth + 4));
                    foreach (var category in categories)
                    {
                        Console.WriteLine($"| {category.PadRight(categoryWidth)} |");
                    }
                    Console.WriteLine(new string('-', categoryWidth + 4));
                }
                else
                {
                    Console.WriteLine("No categories found.");
                }

                Console.WriteLine("\nOptions:");
                Console.WriteLine("1. Type 'c' to enter Product ID or Name directly.");
                Console.WriteLine("2. Type 'i [category name]' to view products in a specific category.");
                Console.WriteLine("3. Type 'i a' to view all inventories.");
                Console.WriteLine("4. Type 'cart' to view your cart and proceed to checkout.");
                Console.WriteLine("5. Type 'b' to return to the previous menu.");
                Console.Write("\nEnter your choice: ");
                string input = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrEmpty(input)) continue;

                if (input == "c")
                {
                    AddToCart();
                }
                else if (input.StartsWith("i "))
                {
                    string categoryName = input.Substring(2).Trim();

                    if (categoryName == "a")
                    {
                        // Display all inventories
                        Console.WriteLine("\nAll Inventories:");
                        ShowInventory(products); // Pass the full product list
                    }
                    else
                    {
                        // View products in the specified category
                        var categoryProducts = products.Where(p => p.Category.Equals(categoryName, StringComparison.OrdinalIgnoreCase)).ToList();

                        if (categoryProducts.Any())
                        {
                            Console.WriteLine($"\nProducts in '{categoryName}' category:");
                            ShowInventory(categoryProducts); // Pass the filtered list
                        }
                        else
                        {
                            Console.WriteLine($"Category '{categoryName}' not found.");
                            Pause();
                        }
                    }
                }
                else if (input == "cart")
                {
                    // Directly view cart
                    Console.Clear();
                    Console.WriteLine("Your Cart:");
                    ShowCurrentCart();

                    if (cart.Any())
                    {
                        Console.WriteLine("\nWould you like to checkout now? (yes/no): ");
                        string confirm = Console.ReadLine()?.Trim().ToLower();
                        if (confirm == "yes")
                        {
                            Checkout();
                            checkout = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Your cart is empty. Returning to the menu.");
                        Pause();
                    }
                }
                else if (input == "b")
                {
                    // Go back to the previous menu
                    Console.WriteLine("Returning to the previous menu...");
                    return;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please try again.");
                }
            }
        }



        private void AddToCart()
        {
            while (true)
            {
                Console.Clear();

                Console.WriteLine("Your Cart:");
                ShowCurrentCart();

                Console.Write("\nEnter Product ID or Name (or type 'b' to go back): ");
                string productInput = Console.ReadLine()?.Trim();

                if (productInput.Equals("b", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Returning to the previous menu...");
                    return;
                }

                var product = products.FirstOrDefault(p =>
                    p.ID.Equals(productInput, StringComparison.OrdinalIgnoreCase) ||
                    p.Name.Equals(productInput, StringComparison.OrdinalIgnoreCase));

                if (product == null)
                {
                    Console.WriteLine("Product not found. Please try again.");
                    Pause();
                }
                else
                {
                    Console.Write($"Enter quantity for {product.Name} (Available: {product.Quantity}): ");
                    if (int.TryParse(Console.ReadLine(), out int quantity) && quantity > 0 && quantity <= product.Quantity)
                    {
                        var cartItem = cart.FirstOrDefault(p => p.Name.Equals(product.Name, StringComparison.OrdinalIgnoreCase));
                        if (cartItem != null)
                        {
                            cartItem.Quantity += quantity; // Update quantity if product already in cart
                        }
                        else
                        {
                            cart.Add(new Product
                            {
                                ID = product.ID,
                                Name = product.Name,
                                Category = product.Category,
                                Quantity = quantity,
                                Price = product.Price
                            });
                        }

                        Console.WriteLine($"{quantity} units of {product.Name} added to the cart.");
                    }
                    else
                    {
                        Console.WriteLine("Invalid quantity. Please try again.");
                    }

                    while (true)
                    {
                        Console.Clear();

                        Console.WriteLine("Your Cart:");
                        ShowCurrentCart();

                        Console.WriteLine("\nOptions:");
                        Console.WriteLine("1. Add more products.");
                        Console.WriteLine("2. Clear the cart.");
                        Console.WriteLine("3. Checkout.");
                        Console.WriteLine("4. Go back to category selection.");
                        Console.Write("\nEnter your choice (1-4): ");
                        string option = Console.ReadLine()?.Trim();

                        if (option == "1")
                        {
                            break; // Go back to add more products
                        }
                        else if (option == "2")
                        {
                            ClearCart();
                            break; // Refresh after clearing cart
                        }
                        else if (option == "3")
                        {
                            Checkout();
                            return; // Proceed to checkout
                        }
                        else if (option == "4")
                        {
                            return; // Go back to category selection
                        }
                        else
                        {
                            Console.WriteLine("Invalid input. Please try again.");
                            Pause();
                        }
                    }
                }
            }
        }


        private void ShowCurrentCart()
        {
            if (cart.Any())
            {
                // Aggregate cart items by Name to combine quantities
                var aggregatedCart = cart
                    .GroupBy(p => p.Name)
                    .Select(group => new Product
                    {
                        ID = group.First().ID, // Keep the first product's ID
                        Name = group.Key,
                        Category = group.First().Category,
                        Quantity = group.Sum(p => p.Quantity), // Sum quantities
                        Price = group.First().Price // Assume the price is the same for all items with the same name
                    }).ToList();

                ShowInventory(aggregatedCart); // Use the same inventory display method
            }
            else
            {
                Console.WriteLine("Your cart is empty.");
            }
        }


        private void ShowInventory(List<Product> inventory)
        {
            // Dynamically calculate column widths for proper alignment
            int idWidth = Math.Max(4, inventory.Max(p => p.ID.Length));
            int nameWidth = Math.Max(10, inventory.Max(p => p.Name.Length));
            int categoryWidth = Math.Max(10, inventory.Max(p => p.Category.Length));
            int quantityWidth = 8;
            int priceWidth = 10;

            // Total width for table formatting
            int totalWidth = idWidth + nameWidth + categoryWidth + quantityWidth + priceWidth + 17;

            Console.WriteLine(new string('-', totalWidth));
            Console.WriteLine($"| {"ID".PadRight(idWidth)} | {"Name".PadRight(nameWidth)} | {"Category".PadRight(categoryWidth)} | {"Quantity".PadLeft(quantityWidth)} | {"Price".PadLeft(priceWidth)} |");
            Console.WriteLine(new string('-', totalWidth));

            foreach (var product in inventory)
            {
                Console.WriteLine($"| {product.ID.PadRight(idWidth)} | {product.Name.PadRight(nameWidth)} | {product.Category.PadRight(categoryWidth)} | {product.Quantity.ToString().PadLeft(quantityWidth)} | {product.Price.ToString("0.00").PadLeft(priceWidth)} |");
            }

            Console.WriteLine(new string('-', totalWidth));
            Pause();
        }

        private void ShowCart()
        {
            while (true)
            {
                Console.Clear();

                Console.WriteLine("Your Cart:");

                if (cart.Any())
                {
                    // Aggregate cart items by Name to combine quantities
                    var aggregatedCart = cart
                        .GroupBy(p => p.Name)
                        .Select(group => new Product
                        {
                            ID = group.First().ID, // Keep the first product's ID
                            Name = group.Key,
                            Category = group.First().Category,
                            Quantity = group.Sum(p => p.Quantity), // Sum quantities
                            Price = group.First().Price // Assume the price is the same for all items with the same name
                        }).ToList();

                    // Show the cart
                    ShowInventory(aggregatedCart); // Use the same inventory display method
                }
                else
                {
                    Console.WriteLine("Your cart is empty.");
                }

                // Show options below the cart
                Console.WriteLine("\nOptions:");
                Console.WriteLine("1. Add more products.");
                Console.WriteLine("2. Clear the cart.");
                Console.WriteLine("3. Checkout.");
                Console.WriteLine("4. Go back to category selection.");
                Console.Write("\nEnter your choice (1-4): ");
                string choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        return; // Go back to add more products
                    case "2":
                        ClearCart();
                        break; // Refresh the screen after clearing
                    case "3":
                        Checkout();
                        return; // Proceed to checkout
                    case "4":
                        return; // Go back to category selection
                    default:
                        Console.WriteLine("Invalid input. Please try again.");
                        Pause();
                        break; // Refresh the screen for invalid input
                }
            }
        }



        private void ClearCart()
        {
            cart.Clear();
            Console.WriteLine("Cart cleared.");
            Pause();
        }

        private void Checkout()
        {
            if (!cart.Any())
            {
                Console.WriteLine("Your cart is empty. Cannot proceed to checkout.");
                Pause();
                return;
            }

            Console.Clear();
            Console.WriteLine("Order Summary:");

            // Aggregate cart items by Name to combine quantities
            var aggregatedCart = cart
                .GroupBy(p => p.Name)
                .Select(group => new Product
                {
                    ID = group.First().ID,
                    Name = group.Key,
                    Category = group.First().Category,
                    Quantity = group.Sum(p => p.Quantity),
                    Price = group.First().Price
                }).ToList();

            // Display aggregated cart
            ShowInventory(aggregatedCart);

            // Calculate total price
            decimal totalPrice = aggregatedCart.Sum(item => item.Quantity * item.Price);

            Console.WriteLine(new string('-', 50));
            Console.WriteLine($"{"Total Price:",-20} {totalPrice,10:0.00} PHP");
            Console.WriteLine(new string('-', 50));

            // Prompt for cash amount with a cancel option
            decimal cashAmount = 0;
            while (true)
            {
                Console.Write("Enter cash amount (or type 'cancel' to abort): ");
                string cashInput = Console.ReadLine()?.Trim().ToLower();
                if (cashInput == "cancel")
                {
                    Console.WriteLine("Checkout canceled.");
                    RestoreInventoryFromCart(cart); // Restore inventory as the transaction is canceled
                    Pause();
                    return;
                }
                if (decimal.TryParse(cashInput, out cashAmount) && cashAmount >= totalPrice)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid amount or insufficient cash. Please try again.");
                }
            }

            decimal change = cashAmount - totalPrice;
            Console.WriteLine($"Change: {change:0.00} PHP");

            foreach (var item in aggregatedCart)
            {
                var product = products.FirstOrDefault(p => p.ID == item.ID);
                if (product != null)
                {
                    product.Quantity -= item.Quantity;
                }
            }
            SaveProductsToCsv();

            // Update or create the monthly sales report
            string reportPath = Path.Combine("reports", $"Monthly_Sales_Report_{DateTime.Now:yyyy_MM}.pdf");
            UpdateSalesReport(reportPath, aggregatedCart);

            // Prompt for receipt options until a valid choice is made
            string receiptOption = null;
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Order Summary:");
                ShowInventory(aggregatedCart);
                Console.WriteLine(new string('-', 50));
                Console.WriteLine($"{"Total Price:",-20} {totalPrice,10:0.00} PHP");
                Console.WriteLine($"Change: {change:0.00} PHP");
                Console.WriteLine(new string('-', 50));

                Console.WriteLine("\nSelect Receipt Option:");
                Console.WriteLine("1. Text-Based Console Receipt");
                Console.WriteLine("2. PDF Receipt");
                Console.WriteLine("3. Do Not Print Receipt");
                Console.Write("Enter your choice (1-3 or type 'cancel' to abort): ");
                receiptOption = Console.ReadLine()?.Trim().ToLower();

                if (receiptOption == "1")
                {
                    PrintConsoleReceipt(aggregatedCart, totalPrice, cashAmount, change);
                    break;
                }
                else if (receiptOption == "2")
                {
                    string receiptPath = Path.Combine("receipts", $"Receipt_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    GeneratePdfReceipt(receiptPath, aggregatedCart, totalPrice, cashAmount, change);
                    break;
                }
                else if (receiptOption == "3")
                {
                    Console.WriteLine("No receipt will be printed.");
                    break;
                }
                else if (receiptOption == "cancel")
                {
                    Console.WriteLine("Checkout process aborted. Inventory restored.");
                    RestoreInventoryFromCart(cart);
                    Pause();
                    return;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please select a valid option.");
                }
            }

            // Clear the cart and show success message
            cart.Clear();
            Console.WriteLine("Checkout complete. Inventory has been updated, and sales report has been updated. Thank you for your order!");
            Pause();
        }




        private void PrintConsoleReceipt(List<Product> purchasedItems, decimal totalPrice, decimal cashAmount, decimal change)
        {
            Console.Clear();
            Console.WriteLine("===============================");
            Console.WriteLine("       MARITES STORE RECEIPT       ");
            Console.WriteLine("===============================");
            Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd} Time: {DateTime.Now:HH:mm:ss}");

            Console.WriteLine("\nItem Name       Qty   Unit   Total");
            Console.WriteLine("-----------------------------------");
            foreach (var product in purchasedItems)
            {
                Console.WriteLine($"{product.Name,-15} {product.Quantity,-5} {product.Price,-6:0.00} {product.Quantity * product.Price,6:0.00}");
            }

            Console.WriteLine("-----------------------------------");
            Console.WriteLine($"Subtotal:                {totalPrice:0.00} PHP");
            Console.WriteLine($"Cash:                    {cashAmount:0.00} PHP");
            Console.WriteLine($"Change:                  {change:0.00} PHP");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Thank you for shopping with us!");
            Console.WriteLine("===============================");
            Pause();
        }

        private void GeneratePdfReceipt(string receiptPath, List<Product> purchasedItems, decimal totalPrice, decimal cashAmount, decimal change)
        {
            try
            {
                using (PdfDocument document = new PdfDocument())
                {
                    PdfPage page = document.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);
                    XFont boldFont = new XFont("Arial", 14);
                    XFont font = new XFont("Arial", 12);

                    // Header
                    gfx.DrawString("Marites Store Receipt", boldFont, XBrushes.Black, new XPoint(40, 40));
                    gfx.DrawString($"Date: {DateTime.Now:yyyy-MM-dd} Time: {DateTime.Now:HH:mm:ss}", font, XBrushes.Black, new XPoint(40, 60));

                    // Order Details
                    double yPosition = 100;
                    gfx.DrawString("Item Name", font, XBrushes.Black, new XPoint(40, yPosition));
                    gfx.DrawString("Qty", font, XBrushes.Black, new XPoint(200, yPosition));
                    gfx.DrawString("Unit Price", font, XBrushes.Black, new XPoint(260, yPosition));
                    gfx.DrawString("Total", font, XBrushes.Black, new XPoint(350, yPosition));
                    yPosition += 20;

                    foreach (var product in purchasedItems)
                    {
                        gfx.DrawString(product.Name, font, XBrushes.Black, new XPoint(40, yPosition));
                        gfx.DrawString(product.Quantity.ToString(), font, XBrushes.Black, new XPoint(200, yPosition));
                        gfx.DrawString(product.Price.ToString("0.00"), font, XBrushes.Black, new XPoint(260, yPosition));
                        gfx.DrawString((product.Quantity * product.Price).ToString("0.00"), font, XBrushes.Black, new XPoint(350, yPosition));
                        yPosition += 20;
                    }

                    // Summary
                    yPosition += 10;
                    gfx.DrawString($"Subtotal: {totalPrice:0.00} PHP", boldFont, XBrushes.Black, new XPoint(260, yPosition));
                    yPosition += 20;
                    gfx.DrawString($"Cash: {cashAmount:0.00} PHP", font, XBrushes.Black, new XPoint(260, yPosition));
                    yPosition += 20;
                    gfx.DrawString($"Change: {change:0.00} PHP", font, XBrushes.Black, new XPoint(260, yPosition));
                    yPosition += 40;

                    gfx.DrawString("Thank you for shopping with us!", font, XBrushes.Black, new XPoint(40, yPosition));

                    // Save the PDF
                    document.Save(receiptPath);
                }

                Console.WriteLine($"Receipt saved: {receiptPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating receipt: {ex.Message}");
            }
        }


        private void UpdateSalesReport(string reportPath, List<Product> products)
        {
            try
            {
                Dictionary<string, (int quantity, decimal total)> salesData = new Dictionary<string, (int quantity, decimal total)>();

                if (File.Exists(reportPath))
                {
                    using (StreamReader reader = new StreamReader(reportPath.Replace(".pdf", ".txt")))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split('|');
                            if (parts.Length == 3)
                            {
                                string productName = parts[0];
                                int quantity = int.Parse(parts[1]);
                                decimal total = decimal.Parse(parts[2]);

                                salesData[productName] = (quantity, total);
                            }
                        }
                    }
                }
                foreach (var product in products)
                {
                    if (salesData.ContainsKey(product.Name))
                    {
                        salesData[product.Name] = (
                            salesData[product.Name].quantity + product.Quantity,
                            salesData[product.Name].total + (product.Quantity * product.Price)
                        );
                    }
                    else
                    {
                        salesData[product.Name] = (product.Quantity, product.Quantity * product.Price);
                    }
                }

                // Write the updated sales data to the PDF
                using (PdfSharp.Pdf.PdfDocument document = new PdfSharp.Pdf.PdfDocument())
                {
                    PdfSharp.Pdf.PdfPage page = document.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    XFont boldFont = new XFont("Arial", 14);
                    XFont font = new XFont("Arial", 12);

                    gfx.DrawString("Monthly Sales Report", boldFont, XBrushes.Black, new XPoint(40, 40));
                    gfx.DrawString($"Date: {DateTime.Now:MMMM yyyy}", font, XBrushes.Black, new XPoint(40, 60));

                    double yPosition = 100;

                    // Write updated sales data
                    foreach (var sale in salesData)
                    {
                        gfx.DrawString(sale.Key, font, XBrushes.Black, new XPoint(40, yPosition));
                        gfx.DrawString(sale.Value.quantity.ToString(), font, XBrushes.Black, new XPoint(200, yPosition));
                        gfx.DrawString(sale.Value.total.ToString("0.00"), font, XBrushes.Black, new XPoint(320, yPosition));
                        yPosition += 20;
                    }

                    // Save the PDF
                    document.Save(reportPath);

                    using (StreamWriter writer = new StreamWriter(reportPath.Replace(".pdf", ".txt"), false))
                    {
                        foreach (var sale in salesData)
                        {
                            writer.WriteLine($"{sale.Key}|{sale.Value.quantity}|{sale.Value.total}");
                        }
                    }
                }

                //Console.WriteLine($"Sales report successfully updated: {reportPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating or updating sales report: {ex.Message}");
            }
        }


        private void RestoreInventoryFromCart(List<Product> aggregatedCart)
        {
            foreach (var item in aggregatedCart)
            {
                var product = products.FirstOrDefault(p => p.ID == item.ID);
                if (product != null)
                {
                    product.Quantity += item.Quantity;
                }
            }
        }


        private void AddProduct()
        {
            string id, name, category;
            int quantity = 0;
            decimal price = 0;

            Console.WriteLine("Add Product - Type 'exit' anytime to return to the main menu.");

            do
            {
                Console.Write("Enter Product ID: ");
                id = Console.ReadLine();
                if (id.ToLower() == "exit") return;

                if (string.IsNullOrWhiteSpace(id))
                {
                    Console.WriteLine("Product ID cannot be empty. Please try again.");
                }
                else if (products.Any(p => p.ID == id))
                {
                    Console.WriteLine("Product ID already exists. Please enter a unique ID.");
                    id = null; // Reset id to repeat the loop
                }
            } while (string.IsNullOrWhiteSpace(id));

            do
            {
                Console.Write("Enter Product Name: ");
                name = Console.ReadLine();
                if (name.ToLower() == "exit") return;
                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine("Product Name cannot be empty. Please try again.");
                }
            } while (string.IsNullOrWhiteSpace(name));

            do
            {
                Console.Write("Enter Product Category: ");
                category = Console.ReadLine();
                if (category.ToLower() == "exit") return;
                if (string.IsNullOrWhiteSpace(category))
                {
                    Console.WriteLine("Product Category cannot be empty. Please try again.");
                }
            } while (string.IsNullOrWhiteSpace(category));

            while (true)
            {
                Console.Write("Enter Quantity: ");
                string quantityInput = Console.ReadLine();
                if (quantityInput.ToLower() == "exit") return;
                if (int.TryParse(quantityInput, out quantity) && quantity >= 0)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Quantity must be a non-negative integer.");
                }
            }

            while (true)
            {
                Console.Write("Enter Price: ");
                string priceInput = Console.ReadLine();
                if (priceInput.ToLower() == "exit") return;
                if (decimal.TryParse(priceInput, out price) && price >= 0)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Price must be a non-negative decimal value.");
                }
            }

            products.Add(new Product { ID = id, Name = name, Category = category, Quantity = quantity, Price = price });
            Console.WriteLine("Product added successfully!");

            Pause();
        }


        private void UpdateProduct()
        {
            Console.Write("Enter Product ID to update: ");
            string id = Console.ReadLine();
            var product = products.FirstOrDefault(p => p.ID == id);

            if (product != null)
            {
                Console.Write("Enter New Name (leave blank to keep current): ");
                string name = Console.ReadLine();
                Console.Write("Enter New Category (leave blank to keep current): ");
                string category = Console.ReadLine();
                Console.Write("Enter New Quantity (leave blank to keep current): ");
                string quantityInput = Console.ReadLine();
                Console.Write("Enter New Price (leave blank to keep current): ");
                string priceInput = Console.ReadLine();

                if (!string.IsNullOrEmpty(name)) product.Name = name;
                if (!string.IsNullOrEmpty(category)) product.Category = category;
                if (int.TryParse(quantityInput, out int quantity)) product.Quantity = quantity;
                if (decimal.TryParse(priceInput, out decimal price)) product.Price = price;

                Console.WriteLine("Product updated successfully.");
            }
            else
            {
                Console.WriteLine("Product not found.");
            }

            Pause();
        }

        private void DeleteProduct()
        {
            Console.Write("Enter Product ID to delete: ");
            string id = Console.ReadLine();
            var product = products.FirstOrDefault(p => p.ID == id);

            if (product != null)
            {
                products.Remove(product);
                Console.WriteLine("Product deleted successfully.");
            }
            else
            {
                Console.WriteLine("Product not found.");
            }

            Pause();
        }

        private void ViewInventory()
        {
            Console.Clear();

            if (!products.Any())
            {
                Console.WriteLine("No products found in inventory.");
                Pause();
                return;
            }

            // Define column widths dynamically or set minimum widths
            int idWidth = Math.Max(4, products.Max(p => p.ID.Length));
            int nameWidth = Math.Max(10, products.Max(p => p.Name.Length));
            int categoryWidth = Math.Max(10, products.Max(p => p.Category.Length));
            int quantityWidth = 8;
            int priceWidth = 10;

            // Calculate total table width
            int totalWidth = idWidth + nameWidth + categoryWidth + quantityWidth + priceWidth + 17;

            // Print table header
            Console.WriteLine(new string('-', totalWidth));
            Console.WriteLine($"| {"ID".PadRight(idWidth)} | {"Name".PadRight(nameWidth)} | {"Category".PadRight(categoryWidth)} | {"Quantity".PadLeft(quantityWidth)} | {"Price".PadLeft(priceWidth)} |");
            Console.WriteLine(new string('-', totalWidth));

            // Print each product in the table
            foreach (var product in products)
            {
                Console.WriteLine($"| {product.ID.PadRight(idWidth)} | {product.Name.PadRight(nameWidth)} | {product.Category.PadRight(categoryWidth)} | {product.Quantity.ToString().PadLeft(quantityWidth)} | {product.Price.ToString("0.00").PadLeft(priceWidth)} |");
            }

            // Print table footer
            Console.WriteLine(new string('-', totalWidth));
            Pause();
        }


        private void SearchProduct()
        {
            Console.Write("Enter Product ID or Name to search: ");
            string searchInput = Console.ReadLine();

            var results = products.Where(p =>
                p.ID.Equals(searchInput, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(searchInput, StringComparison.OrdinalIgnoreCase)).ToList();

            if (results.Any())
            {
                int idWidth = Math.Max(4, results.Max(p => p.ID.Length));
                int nameWidth = Math.Max(10, results.Max(p => p.Name.Length));
                int categoryWidth = Math.Max(10, results.Max(p => p.Category.Length));
                int quantityWidth = 8;
                int priceWidth = 10;

                int totalWidth = idWidth + nameWidth + categoryWidth + quantityWidth + priceWidth + 17;

                Console.WriteLine(new string('-', totalWidth));
                Console.WriteLine($"| {"ID".PadRight(idWidth)} | {"Name".PadRight(nameWidth)} | {"Category".PadRight(categoryWidth)} | {"Quantity".PadLeft(quantityWidth)} | {"Price".PadLeft(priceWidth)} |");
                Console.WriteLine(new string('-', totalWidth));

                foreach (var product in results)
                {
                    Console.WriteLine($"| {product.ID.PadRight(idWidth)} | {product.Name.PadRight(nameWidth)} | {product.Category.PadRight(categoryWidth)} | {product.Quantity.ToString().PadLeft(quantityWidth)} | {product.Price.ToString("0.00").PadLeft(priceWidth)} |");
                }

                Console.WriteLine(new string('-', totalWidth));
            }
            else
            {
                Console.WriteLine("No matching products found.");
            }

            Pause();
        }


        private void LoadProductsFromCsv()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    products = File.ReadAllLines(FilePath)
                        .Skip(1) 
                        .Select(Product.FromCsv)
                        .Where(product => product != null)
                        .ToList();
                }
                else
                {
                    Console.WriteLine("No inventory file found. Starting with an empty inventory.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading products from CSV: {ex.Message}");
            }
        }

        private void SaveProductsToCsv()
        {
            try
            {
                File.WriteAllLines(FilePath, new[] { "ID,Name,Category,Quantity,Price" }
                    .Concat(products.Select(p => p.ToString())));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving inventory file: {ex.Message}");
            }
        }

        private void Pause()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}