using CustomerApi.Application.DTOs;
using CustomerApi.Application.Interfaces;
using CustomerApi.Application.Services;
using CustomerApi.Application.Validators;
using CustomerApi.Domain.Entities;
using CustomerApi.Domain.Exceptions;
using CustomerApi.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace CustomerApi.Tests.Services;

public sealed class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _repository = new();
    private readonly Mock<ICustomerCache> _cache = new();
    private readonly CustomerService  _customerService;

    public CustomerServiceTests()
    {
        _customerService = new CustomerService(
            _repository.Object,
            _cache.Object,
            new CreateCustomerRequestValidator(),
            new UpdateCustomerRequestValidator());
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerExistsInCache_ReturnsCachedCustomer()
    {
        var customer = BuildCustomer();
        _cache.Setup(c => c.GetAsync(customer.Id, default)).ReturnsAsync(customer);

        var result = await _customerService.GetByIdAsync(customer.Id);

        result.Id.Should().Be(customer.Id);
        _repository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMiss_QueriesRepositoryAndPopulatesCache()
    {
        var customer = BuildCustomer();
        _cache.Setup(c => c.GetAsync(customer.Id, default)).ReturnsAsync((Customer?)null);
        _repository.Setup(r => r.GetByIdAsync(customer.Id, default)).ReturnsAsync(customer);

        var result = await _customerService.GetByIdAsync(customer.Id);

        result.Id.Should().Be(customer.Id);
        _cache.Verify(c => c.SetAsync(customer, default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerNotFound_ThrowsCustomerNotFoundException()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Customer?)null);
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Customer?)null);

        var act = () => _customerService.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<CustomerNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsCreatedCustomer()
    {
        var request = new CreateCustomerRequest("Jane", "Doe", "jane@example.com", 30);
        _repository.Setup(r => r.GetByEmailAsync(request.Email, default)).ReturnsAsync((Customer?)null);
        _repository.Setup(r => r.CreateAsync(It.IsAny<Customer>(), default))
                   .ReturnsAsync((Customer c, CancellationToken _) => c);

        var result = await _customerService.CreateAsync(request);

        result.Email.Should().Be("jane@example.com");
        result.FirstName.Should().Be("Jane");
        _cache.Verify(c => c.SetAsync(It.IsAny<Customer>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsDuplicateEmailException()
    {
        var existing = BuildCustomer();
        var request = new CreateCustomerRequest("Jane", "Doe", existing.Email, 30);
        _repository.Setup(r => r.GetByEmailAsync(request.Email, default)).ReturnsAsync(existing);

        var act = () => _customerService.CreateAsync(request);

        await act.Should().ThrowAsync<DuplicateEmailException>();
    }

    [Theory]
    [InlineData("", "Doe", "jane@example.com", 30)]
    [InlineData("Jane", "", "jane@example.com", 30)]
    [InlineData("Jane", "Doe", "not-an-email", 30)]
    [InlineData("Jane", "Doe", "jane@example.com", 17)]
    [InlineData("Jane", "Doe", "jane@example.com", 121)]
    public async Task CreateAsync_WithInvalidInput_ThrowsValidationException(string firstName, string lastName, string email, int age)
    {
        var request = new CreateCustomerRequest(firstName, lastName, email, age);

        var act = () => _customerService.CreateAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ReturnsUpdatedCustomer()
    {
        var customer = BuildCustomer();
        var request = new UpdateCustomerRequest("Updated", "Name", customer.Email, 35);

        _cache.Setup(c => c.GetAsync(customer.Id, default)).ReturnsAsync((Customer?)null);
        _repository.Setup(r => r.GetByIdAsync(customer.Id, default)).ReturnsAsync(customer);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<Customer>(), default))
                   .ReturnsAsync((Customer c, CancellationToken _) => c);

        var result = await _customerService.UpdateAsync(customer.Id, request);

        result.FirstName.Should().Be("Updated");
        result.Age.Should().Be(35);
        _cache.Verify(c => c.SetAsync(It.IsAny<Customer>(), default), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCustomerNotFound_ThrowsCustomerNotFoundException()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Customer?)null);
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Customer?)null);

        var act = () => _customerService.UpdateAsync(Guid.NewGuid(), new UpdateCustomerRequest("A", "B", "a@b.com", 30));

        await act.Should().ThrowAsync<CustomerNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithEmailAlreadyTakenByAnother_ThrowsDuplicateEmailException()
    {
        var existing = BuildCustomer();
        var other = BuildCustomer(email: "other@example.com");
        var request = new UpdateCustomerRequest("Jane", "Doe", other.Email, 30);

        _cache.Setup(c => c.GetAsync(existing.Id, default)).ReturnsAsync((Customer?)null);
        _repository.Setup(r => r.GetByIdAsync(existing.Id, default)).ReturnsAsync(existing);
        _repository.Setup(r => r.GetByEmailAsync(other.Email, default)).ReturnsAsync(other);

        var act = () => _customerService.UpdateAsync(existing.Id, request);

        await act.Should().ThrowAsync<DuplicateEmailException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenCustomerExists_DeletesAndEvictsCache()
    {
        var customer = BuildCustomer();
        _repository.Setup(r => r.ExistsAsync(customer.Id, default)).ReturnsAsync(true);

        await _customerService.DeleteAsync(customer.Id);

        _repository.Verify(r => r.DeleteAsync(customer.Id, default), Times.Once);
        _cache.Verify(c => c.RemoveAsync(customer.Id, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenCustomerNotFound_ThrowsCustomerNotFoundException()
    {
        _repository.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), default)).ReturnsAsync(false);

        var act = () => _customerService.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<CustomerNotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_WithFirstNameFilter_DelegatesToRepository()
    {
        var matched = new List<Customer>
        {
            BuildCustomer(firstName: "Alice"),
            BuildCustomer(firstName: "Alice"),
        };
        _repository.Setup(r => r.GetAllAsync("alice", default)).ReturnsAsync(matched);

        var result = await _customerService.GetAllAsync("alice", 1, 10);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        _repository.Verify(r => r.GetAllAsync("alice", default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithPaging_ReturnsCorrectPage()
    {
        var customers = Enumerable.Range(1, 15)
            .Select(i => BuildCustomer(firstName: $"Customer{i}"))
            .ToList();
        _repository.Setup(r => r.GetAllAsync(null, default)).ReturnsAsync(customers);

        var result = await _customerService.GetAllAsync(null, 2, 5);

        result.Items.Should().HaveCount(5);
        result.Page.Should().Be(2);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    private static Customer BuildCustomer(string firstName = "John", string lastName = "Doe", string email = "john@example.com",int age = 30) =>
        new()
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Age = age,
        };
}