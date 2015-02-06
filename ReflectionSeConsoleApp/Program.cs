using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionSeConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var category = GenerateRandomList();

            Object value;

            value = ReflectionUtil.GetPropertyValue(category, "Name");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(category, "Products");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(category, "Products.@last.Name");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(category, "Products.@78.DateOfManufacture.Year");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(category, "Products.@first.Components.@Key_1.Name");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(category, "Products.@78.Components.@Key_3.Name");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(category, "Products.@last.Components.@Key_1.Name");
            Console.WriteLine(value);

            ReflectionUtil.SetPropertyValue(category, "Products.@16.Components.@Key_1.Name", "New Name 1");
            Console.WriteLine(value);

            ReflectionUtil.SetPropertyValue(category, "Products.@last.Components.@Key_1.Name", "New Name 2");
            Console.WriteLine(value);

            var list = category.Products;

            value = ReflectionUtil.GetPropertyValue(list, "@first.Name");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(list, "@last.Name");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(list, "@78.DateOfManufacture.Year");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(list, "@first.Components.@Key_5.Name");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(list, "@78.Components.@Key_3.Name");
            Console.WriteLine(value);

            value = ReflectionUtil.GetPropertyValue(list, "@last.Components.@Key_1.Name");
            Console.WriteLine(value);

            ReflectionUtil.SetPropertyValue(list, "@16.Components.@Key_5.Name", "New Name 4");
            Console.WriteLine(value);

            ReflectionUtil.SetPropertyValue(list, "@last.Components.@Key_5.Name", "New Name 5");
            Console.WriteLine(value);
        }



        static Category GenerateRandomList()
        {
            var category = new Category();
            category.Name = "Test Category";
            category.Products = new List<Product>();

            for (int i = 0; i < 100; i++)
            {
                var p = new Product
                {
                    Name = String.Format("PN{0}", i),
                    DateOfManufacture = DateTime.Now,
                    Components = new Dictionary<string, Component>
                    {
                        {"Key_1", new Component { Name = "Component - 1", Description = "Description - 1" }},
                        {"Key_2", new Component { Name = "Component - 2", Description = "Description - 2" }},
                        {"Key_3", new Component { Name = "Component - 3", Description = "Description - 3" }},
                        {"Key_4", new Component { Name = "Component - 4", Description = "Description - 4" }},
                        {"Key_5", new Component { Name = "Component - 5", Description = "Description - 5" }},
                    }
                };

                category.Products.Add(p);
            }

            return category;
        }

        class Category
        {
            public String Name { get; set; }
            public List<Product> Products { get; set; }
        }

        class Product
        {
            public String Name { get; set; }

            public DateTime? DateOfManufacture { get; set; }

            public Dictionary<String, Component> Components { get; set; }
        }

        class Component
        {
            public String Name { get; set; }

            public String Description { get; set; }
        }
    }
}
