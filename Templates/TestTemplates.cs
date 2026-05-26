namespace ArchitectGenerator.Templates;

public static class TestTemplates
{
	// ============================================================
	// BASE — Gerçek, çalışan örnek testler
	// ============================================================

	public static string BaseDomainTests() =>
"""
using Base.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Base.Domain.Tests;

// Domain entity'lerinin invariant'larını test edersin.
// Şu an BaseEntity sade bir POCO — gerçek invariant'lar
// modüllerdeki entity'lerde test edilir.
public class BaseEntityTests
{
    private class TestEntity : BaseEntity { }

    [Fact]
    public void NewEntity_IsActive_DefaultsToTrue()
    {
        // Arrange
        var entity = new TestEntity();

        // Act + Assert
        entity.IsActive.Should().BeTrue();
    }
}
""";

	public static string BaseApplicationTests() =>
"""
using Base.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Xunit;

namespace Base.Application.Tests;

// ValidationBehavior: MediatR pipeline behavior. Eğer request için
// kayıtlı validator(lar) varsa, hata fırlatır; yoksa next() çalışır.
public class ValidationBehaviorTests
{
    public record TestRequest(string Name) : IRequest<string>;

    [Fact]
    public async Task Handle_NoValidationErrors_CallsNext()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ => { nextCalled = true; return Task.FromResult("ok"); };

        // Act
        var result = await behavior.Handle(new TestRequest("a"), next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var failingValidator = new Mock<IValidator<TestRequest>>();
        failingValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Name", "Name boş olamaz") }));

        var behavior = new ValidationBehavior<TestRequest, string>(new[] { failingValidator.Object });
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        // Act
        var act = async () => await behavior.Handle(new TestRequest(""), next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.ErrorMessage == "Name boş olamaz"));
    }
}
""";

	public static string BaseInfrastructureTests() =>
"""
using Base.Infrastructure.Services.Auth;
using FluentAssertions;
using Xunit;

namespace Base.Infrastructure.Tests;

// PasswordHasher: BCrypt wrapper. Hash + Verify cycle'ı test ediyoruz.
public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesNonEmptyString()
    {
        var hash = _hasher.Hash("Sifre123");
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("Sifre123");
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Sifre123");
        _hasher.Verify("Sifre123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Sifre123");
        _hasher.Verify("YanlisSifre", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_SamePassword_ProducesDifferentHashes()
    {
        // BCrypt her hash'te farklı salt kullanır
        var hash1 = _hasher.Hash("Sifre123");
        var hash2 = _hasher.Hash("Sifre123");
        hash1.Should().NotBe(hash2);
    }
}
""";

	public static string BasePersistenceTests() =>
"""
using Base.Application.Interfaces;
using Base.Domain.Enums;
using Base.Domain.Identity;
using Base.Persistence.DbContext;
using Base.Persistence.Interfaces;
using Base.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Base.Persistence.Tests;

// GenericRepository: AppDbContext üzerinden çalışıyor. Burada
// EF Core InMemory provider ile gerçek DB olmadan test ediyoruz.
// Test entity'si olarak Base'de tanımlı User'ı kullanıyoruz
// (DbContext'e tanıtılmış bir entity olduğu için InMemory provider
// onu modelde bulabilir).
public class GenericRepositoryTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, Array.Empty<IModelConfigurator>());
    }

    private static User NewUser() => new()
    {
        UserName = "test-user",
        PasswordHash = "hash",
        Role = UserRole.Employee
    };

    [Fact]
    public async Task AddAsync_SetsAuditFields()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var userCtx = new Mock<IUserContextService>();
        userCtx.Setup(x => x.GetCurrentUserId()).Returns(42);
        var repo = new GenericRepository<User>(ctx, userCtx.Object);
        var entity = NewUser();

        // Act
        await repo.AddAsync(entity);

        // Assert
        entity.IsActive.Should().BeTrue();
        entity.CreatedById.Should().Be(42);
        entity.CreatedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Delete_MarksEntityInactive()
    {
        // Arrange
        var ctx = CreateInMemoryContext();
        var userCtx = new Mock<IUserContextService>();
        userCtx.Setup(x => x.GetCurrentUserId()).Returns(42);
        var repo = new GenericRepository<User>(ctx, userCtx.Object);
        var entity = NewUser();
        entity.IsActive = true;

        // Act
        repo.Delete(entity);

        // Assert (soft delete)
        entity.IsActive.Should().BeFalse();
        entity.DeletedDate.Should().NotBeNull();
        entity.DeletedById.Should().Be(42);
    }
}
""";

	// ============================================================
	// MODULE — Placeholder, kullanıcının doldurması için pattern
	// ============================================================

	public static string ModuleDomainTests(string moduleName) =>
$$"""
using FluentAssertions;
using Xunit;

namespace {{moduleName}}.Domain.Tests;

// Domain entity invariant'larını burada test et.
// Örnek (kendi entity'n için uyarla):
//
//   [Fact]
//   public void Order_Total_MustBePositive()
//   {
//       var act = () => new Order(amount: -1);
//       act.Should().Throw<ArgumentException>();
//   }

public class PlaceholderTests
{
    [Fact]
    public void Placeholder()
    {
        // Buraya kendi testlerini ekle; bu testi sil.
        true.Should().BeTrue();
    }
}
""";

	public static string ModuleApplicationTests(string moduleName) =>
$$"""
using FluentAssertions;
using Xunit;

namespace {{moduleName}}.Application.Tests;

// Command/Query handler ve Validator testleri buraya gelir.
//
// Tipik handler testi (AAA pattern):
//
//   [Fact]
//   public async Task Handle_ValidRequest_ReturnsExpectedResult()
//   {
//       // Arrange
//       var uow = new Mock<IUnitOfWork>();
//       var handler = new CreateXyzCommandHandler(uow.Object);
//       var cmd = new CreateXyzCommand("Name");
//
//       // Act
//       var result = await handler.Handle(cmd, CancellationToken.None);
//
//       // Assert
//       result.Should().NotBeNull();
//       uow.Verify(x => x.CommitAsync(), Times.Once);
//   }

public class PlaceholderTests
{
    [Fact]
    public void Placeholder()
    {
        // Buraya kendi testlerini ekle; bu testi sil.
        true.Should().BeTrue();
    }
}
""";

	public static string ModuleInfrastructureTests(string moduleName) =>
$$"""
using FluentAssertions;
using Xunit;

namespace {{moduleName}}.Infrastructure.Tests;

// Modüle özel external service implementasyonları için.
// Genelde gerçek dış sistemleri mock'lar veya in-memory implementation
// kullanırsın.

public class PlaceholderTests
{
    [Fact]
    public void Placeholder()
    {
        true.Should().BeTrue();
    }
}
""";

	public static string ModulePersistenceTests(string moduleName) =>
$$"""
using FluentAssertions;
using Xunit;

namespace {{moduleName}}.Persistence.Tests;

// IEntityTypeConfiguration<T> sınıflarının doğru çalıştığını
// EF Core InMemory ile test edebilirsin (Base.Persistence.Tests'e bak).

public class PlaceholderTests
{
    [Fact]
    public void Placeholder()
    {
        true.Should().BeTrue();
    }
}
""";
}
