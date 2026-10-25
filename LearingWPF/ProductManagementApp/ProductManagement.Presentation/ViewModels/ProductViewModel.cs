using MediatR;

using ProductManagement.Presentation.Core;
using ProductManagement.Presentation.Models;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ProductManagement.Presentation.ViewModels
{
    /// <summary>
    /// The ProductViewModel demonstrates Clean Architecture principles
    /// In a real application, this would depend on IMediator or Use Cases from Application layer
    /// For learning purposes, this demonstrates the presentation layer structure
    /// </summary>
    public class ProductViewModel : INotifyPropertyChanged
    {
        private readonly IMediator _mediator;
        private string _productName = string.Empty;
        private string _productQuantity = string.Empty;
        private bool _isBusy;

        public ObservableCollection<Product> Products { get; } = new();

        public string ProductName
        {
            get => _productName;
            set { _productName = value; OnPropertyChanged(); }
        }

        public string ProductQuantity
        {
            get => _productQuantity;
            set { _productQuantity = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        // Commands that would eventually delegate to MediatR handlers or Use Cases
        public ICommand LoadProductsCommand { get; }
        public ICommand AddProductCommand { get; }

        public ProductViewModel(IMediator mediator)
        {
            _mediator= mediator;
            // Initialize commands with async methods
            LoadProductsCommand = new RelayCommandAsync(LoadProductsAsync);
            AddProductCommand = new RelayCommandAsync(AddProductAsync, CanAddProduct);
        }

        private bool CanAddProduct(object? param)
        {
            return !IsBusy && 
                   !string.IsNullOrWhiteSpace(ProductName) && 
                   int.TryParse(ProductQuantity, out int quantity) && 
                   quantity >= 0;
        }

        private async Task LoadProductsAsync(object? param)
        {
            SetBusy(true);
            try
            {
                // In a real application, this would call MediatR or a Use Case
                var products = await _mediator.Send(new GetProductsQuery());
                
                // For demonstration purposes, we'll use sample data
                Products.Clear();
                Products.Add(new Product { Id = 1, Name = "Laptop", Quantity = 10 });
                Products.Add(new Product { Id = 2, Name = "Mouse", Quantity = 50 });
                Products.Add(new Product { Id = 3, Name = "Keyboard", Quantity = 30 });
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task AddProductAsync(object? param)
        {
            SetBusy(true);
            try
            {
                // In a real application, this would call MediatR or a Use Case
                var newProduct = await _mediator.Send(new CreateProductCommand(ProductName, int.Parse(ProductQuantity)));

                // For demonstration purposes, we'll create a new product
                var newProduct = new Product 
                { 
                    Id = Products.Count > 0 ? Products[Products.Count - 1].Id + 1 : 1,
                    Name = ProductName, 
                    Quantity = int.Parse(ProductQuantity) 
                };
                
                Products.Add(newProduct);
                
                // Clear input fields
                ProductName = string.Empty;
                ProductQuantity = string.Empty;
              
            }
            catch (Exception ex)
            {
                // In a real app, you'd show an error message to the user
                Console.WriteLine($"Error adding product: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool isBusy)
        {
            IsBusy = isBusy;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}