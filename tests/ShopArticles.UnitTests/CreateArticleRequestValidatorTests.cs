using ShopArticles.Core.Contracts;
using ShopArticles.Core.Validation;

namespace ShopArticles.UnitTests;

public class CreateArticleRequestValidatorTests
{
    private readonly CreateArticleRequestValidator _validator = new();

    private static CreateArticleRequest MemoryStick() => new()
    {
        Name = "Branded Memory Stick",
        Description = "Branded 16 GB memory stick",
        Category = "USB flash drive",
        Price = 17.89m,
        Currency = "NOK",
    };

    [Fact]
    public void Valid_article_passes()
    {
        Assert.True(_validator.Validate(MemoryStick()).IsValid);
    }

    [Fact]
    public void Free_article_without_currency_passes()
    {
        var request = MemoryStick();
        request.Price = 0;
        request.Currency = null;

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Optional_category_can_be_omitted()
    {
        var request = MemoryStick();
        request.Category = null;

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Paid_article_without_currency_fails(string? currency)
    {
        var request = MemoryStick();
        request.Currency = currency;

        AssertSingleError(request, nameof(CreateArticleRequest.Currency));
    }

    [Fact]
    public void Unknown_currency_code_fails()
    {
        var request = MemoryStick();
        request.Currency = "XYZ";

        AssertSingleError(request, nameof(CreateArticleRequest.Currency));
    }

    [Fact]
    public void Client_supplied_id_fails()
    {
        var request = MemoryStick();
        request.ArticleId = 1;

        AssertSingleError(request, nameof(CreateArticleRequest.ArticleId));
    }

    [Fact]
    public void Negative_price_fails()
    {
        var request = MemoryStick();
        request.Price = -0.01m;

        AssertSingleError(request, nameof(CreateArticleRequest.Price));
    }

    [Fact]
    public void Missing_price_fails()
    {
        var request = MemoryStick();
        request.Price = null;

        Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == nameof(CreateArticleRequest.Price));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_name_fails(string? name)
    {
        var request = MemoryStick();
        request.Name = name;

        AssertSingleError(request, nameof(CreateArticleRequest.Name));
    }

    [Theory]
    [InlineData(nameof(CreateArticleRequest.Name), CreateArticleRequestValidator.NameMaxLength)]
    [InlineData(nameof(CreateArticleRequest.Description), CreateArticleRequestValidator.DescriptionMaxLength)]
    [InlineData(nameof(CreateArticleRequest.Category), CreateArticleRequestValidator.CategoryMaxLength)]
    public void Text_longer_than_max_length_fails_but_max_length_passes(string property, int maxLength)
    {
        var atLimit = MemoryStick();
        SetText(atLimit, property, new string('a', maxLength));
        Assert.True(_validator.Validate(atLimit).IsValid);

        var overLimit = MemoryStick();
        SetText(overLimit, property, new string('a', maxLength + 1));
        AssertSingleError(overLimit, property);
    }

    private void AssertSingleError(CreateArticleRequest request, string property)
    {
        var result = _validator.Validate(request);

        var error = Assert.Single(result.Errors);
        Assert.Equal(property, error.PropertyName);
    }

    private static void SetText(CreateArticleRequest request, string property, string value) =>
        typeof(CreateArticleRequest).GetProperty(property)!.SetValue(request, value);
}

public class UpdateArticleRequestValidatorTests
{
    private readonly UpdateArticleRequestValidator _validator = new();

    private static UpdateArticleRequest MemoryStick() => new()
    {
        Name = "Branded Memory Stick",
        Description = "Branded 256 GB memory stick",
        Category = "USB flash drive",
        Price = 31.89m,
        Currency = "NOK",
        Version = 1,
    };

    [Fact]
    public void Valid_update_passes()
    {
        Assert.True(_validator.Validate(MemoryStick()).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    public void Missing_or_invalid_version_fails(long? version)
    {
        var request = MemoryStick();
        request.Version = version;

        var result = _validator.Validate(request);

        Assert.Equal(nameof(UpdateArticleRequest.Version), Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void Article_rules_from_create_still_apply()
    {
        var request = MemoryStick();
        request.Currency = null;

        var result = _validator.Validate(request);

        Assert.Equal(nameof(UpdateArticleRequest.Currency), Assert.Single(result.Errors).PropertyName);
    }
}
