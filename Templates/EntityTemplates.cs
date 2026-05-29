using ArchitectGenerator.Core;

namespace ArchitectGenerator.Templates;

/// <summary>
/// Bir modül içinde tek bir entity için tam CRUD (CQRS) dilimi üretir.
/// Mapping, feature başına bir Converters sınıfı ile elle yapılır (AutoMapper yok).
/// Yazma (Create/Update/Delete) [Authorize], okuma (GetById/GetList) anonim.
/// </summary>
public static class EntityTemplates
{
	private static string Feature(string module, string entity) => $"{module}.Application.Features.{entity}s";

	private static string Indent(IEnumerable<string> lines, string pad)
		=> string.Join("\n", lines.Select(l => pad + l));

	/// <summary>GetList handler'ında alan tipine göre koşullu .And(...) filtre satırları üretir.</summary>
	private static string FilterPredicates(List<EntityField> fields)
	{
		var lines = new List<string>();
		foreach (var f in fields)
		{
			if (f.IsString)
			{
				lines.Add($"        if (!string.IsNullOrWhiteSpace(req.{f.Name}))");
				lines.Add($"            filter = filter.And(x => x.{f.Name} != null && x.{f.Name}.Contains(req.{f.Name}!));");
			}
			else if (f.IsDate)
			{
				lines.Add($"        if (req.{f.Name}From.HasValue)");
				lines.Add($"            filter = filter.And(x => x.{f.Name} >= req.{f.Name}From.Value);");
				lines.Add($"        if (req.{f.Name}To.HasValue)");
				lines.Add($"            filter = filter.And(x => x.{f.Name} <= req.{f.Name}To.Value);");
			}
			else
			{
				lines.Add($"        if (req.{f.Name}.HasValue)");
				lines.Add($"            filter = filter.And(x => x.{f.Name} == req.{f.Name}.Value);");
			}
		}
		return string.Join("\n", lines);
	}

	// ---------- Domain ----------

	public static string Entity(string module, string entity, List<EntityField> fields)
	{
		var props = Indent(fields.Select(f => f.PropertyLine()), "    ");
		return
$$"""
using Base.Domain.Entities;

namespace {{module}}.Domain.Entities;

public class {{entity}} : BaseEntity
{
{{props}}
}
""";
	}

	// ---------- Persistence ----------

	public static string Configuration(string module, string entity, List<EntityField> fields)
	{
		var stringRules = Indent(
			fields.Where(f => f.IsString).Select(f =>
			{
				var required = f.RequiredString ? ".IsRequired()" : "";
				return $"builder.Property(x => x.{f.Name}){required}.HasMaxLength(200);";
			}),
			"        ");

		return
$$"""
using {{module}}.Domain.Entities;
using Base.Persistence.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace {{module}}.Persistence.Configurations;

public class {{entity}}Configuration : BaseEntityConfiguration<{{entity}}>
{
    public override void Configure(EntityTypeBuilder<{{entity}}> builder)
    {
        builder.ToTable("{{entity}}s", "{{module}}");
        base.Configure(builder);

{{stringRules}}
    }
}
""";
	}

	// ---------- Application / Models ----------

	public static string Response(string module, string entity, List<EntityField> fields)
	{
		var props = Indent(fields.Select(f => f.PropertyLine()), "    ");
		return
$$"""
namespace {{Feature(module, entity)}}.Models;

public class {{entity}}Response
{
    public long Id { get; set; }
{{props}}
}
""";
	}

	public static string CreateRequest(string module, string entity, List<EntityField> fields)
	{
		var props = Indent(fields.Select(f => f.PropertyLine()), "    ");
		return
$$"""
namespace {{Feature(module, entity)}}.Models;

public class Create{{entity}}Request
{
{{props}}
}
""";
	}

	public static string UpdateRequest(string module, string entity, List<EntityField> fields)
	{
		var props = Indent(fields.Select(f => f.PropertyLine()), "    ");
		return
$$"""
namespace {{Feature(module, entity)}}.Models;

public class Update{{entity}}Request
{
    public long Id { get; set; }
{{props}}
}
""";
	}

	public static string GetRequest(string module, string entity, List<EntityField> fields)
	{
		var lines = new List<string>();
		foreach (var f in fields)
		{
			if (f.IsString)
				lines.Add($"    public string? {f.Name} {{ get; set; }}");
			else if (f.IsDate)
			{
				lines.Add($"    public {f.BaseType}? {f.Name}From {{ get; set; }}");
				lines.Add($"    public {f.BaseType}? {f.Name}To {{ get; set; }}");
			}
			else
				lines.Add($"    public {f.BaseType}? {f.Name} {{ get; set; }}");
		}
		var props = string.Join("\n", lines);
		return
$$"""
using Base.Application.Common.Models;

namespace {{Feature(module, entity)}}.Models;

// Sayfalama (PagedRequest) + opsiyonel filtreler. Hepsi null ise yalnızca aktif kayıtlar döner.
public class Get{{entity}}Request : PagedRequest
{
{{props}}
}
""";
	}

	// ---------- Application / Converters (elle mapping) ----------

	public static string Converters(string module, string entity, List<EntityField> fields)
	{
		var maps = Indent(fields.Select(f => $"{f.Name} = item.{f.Name},"), "            ");
		return
$$"""
using {{module}}.Domain.Entities;
using {{Feature(module, entity)}}.Models;

namespace {{Feature(module, entity)}}.Converters;

public static class {{entity}}Converters
{
    public static {{entity}}Response {{entity}}Converter({{entity}} item)
    {
        return new {{entity}}Response
        {
            Id = item.Id,
{{maps}}
        };
    }

    public static List<{{entity}}Response> {{entity}}ConverterList(List<{{entity}}> items)
    {
        if (items == null || items.Count == 0)
            return new List<{{entity}}Response>();

        return (from item in items select {{entity}}Converter(item)).ToList();
    }
}
""";
	}

	// ---------- Application / Commands ----------

	public static string CreateCommand(string module, string entity) =>
$$"""
using {{Feature(module, entity)}}.Models;
using MediatR;

namespace {{Feature(module, entity)}}.Commands;

public class Create{{entity}}Command : IRequest<{{entity}}Response>
{
    public Create{{entity}}Request Request { get; }

    public Create{{entity}}Command(Create{{entity}}Request request)
    {
        Request = request;
    }
}
""";

	public static string CreateCommandHandler(string module, string entity, List<EntityField> fields)
	{
		var assigns = Indent(fields.Select(f => $"{f.Name} = dto.{f.Name},"), "            ");
		return
$$"""
using {{Feature(module, entity)}}.Converters;
using {{Feature(module, entity)}}.Models;
using {{module}}.Domain.Entities;
using Base.Domain.Interfaces;
using MediatR;

namespace {{Feature(module, entity)}}.Commands;

public class Create{{entity}}CommandHandler : IRequestHandler<Create{{entity}}Command, {{entity}}Response>
{
    private readonly IUnitOfWork _unitOfWork;

    public Create{{entity}}CommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<{{entity}}Response> Handle(Create{{entity}}Command request, CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var entity = new {{entity}}
        {
{{assigns}}
        };

        await _unitOfWork.Repository<{{entity}}>().AddAsync(entity);
        await _unitOfWork.CommitAsync();

        return {{entity}}Converters.{{entity}}Converter(entity);
    }
}
""";
	}

	public static string UpdateCommand(string module, string entity) =>
$$"""
using {{Feature(module, entity)}}.Models;
using MediatR;

namespace {{Feature(module, entity)}}.Commands;

public class Update{{entity}}Command : IRequest<{{entity}}Response>
{
    public Update{{entity}}Request Request { get; }

    public Update{{entity}}Command(Update{{entity}}Request request)
    {
        Request = request;
    }
}
""";

	public static string UpdateCommandHandler(string module, string entity, List<EntityField> fields)
	{
		var assigns = Indent(fields.Select(f => $"entity.{f.Name} = dto.{f.Name};"), "        ");
		return
$$"""
using {{Feature(module, entity)}}.Converters;
using {{Feature(module, entity)}}.Models;
using {{module}}.Domain.Entities;
using Base.Domain.Interfaces;
using MediatR;

namespace {{Feature(module, entity)}}.Commands;

public class Update{{entity}}CommandHandler : IRequestHandler<Update{{entity}}Command, {{entity}}Response>
{
    private readonly IUnitOfWork _unitOfWork;

    public Update{{entity}}CommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<{{entity}}Response> Handle(Update{{entity}}Command request, CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var entity = await _unitOfWork.Repository<{{entity}}>().GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException("{{entity}} bulunamadı.");

{{assigns}}

        _unitOfWork.Repository<{{entity}}>().Update(entity);
        await _unitOfWork.CommitAsync();

        return {{entity}}Converters.{{entity}}Converter(entity);
    }
}
""";
	}

	public static string DeleteCommand(string module, string entity) =>
$$"""
using MediatR;

namespace {{Feature(module, entity)}}.Commands;

public class Delete{{entity}}Command : IRequest<Unit>
{
    public long Id { get; }

    public Delete{{entity}}Command(long id)
    {
        Id = id;
    }
}
""";

	public static string DeleteCommandHandler(string module, string entity) =>
$$"""
using {{module}}.Domain.Entities;
using Base.Domain.Interfaces;
using MediatR;

namespace {{Feature(module, entity)}}.Commands;

public class Delete{{entity}}CommandHandler : IRequestHandler<Delete{{entity}}Command, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public Delete{{entity}}CommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(Delete{{entity}}Command request, CancellationToken cancellationToken)
    {
        // Soft delete (IsActive = false) — GenericRepository üzerinden
        await _unitOfWork.Repository<{{entity}}>().DeleteByIdAsync(request.Id);
        await _unitOfWork.CommitAsync();

        return Unit.Value;
    }
}
""";

	// ---------- Application / Validators ----------

	public static string CreateValidator(string module, string entity, List<EntityField> fields)
	{
		var rules = fields.Where(f => f.RequiredString)
			.Select(f => $"        RuleFor(x => x.Request.{f.Name}).NotEmpty().WithMessage(\"{f.Name} boş olamaz.\");");
		var body = string.Join("\n", rules);
		return
$$"""
using {{Feature(module, entity)}}.Commands;
using FluentValidation;

namespace {{Feature(module, entity)}}.Validators;

public class Create{{entity}}CommandValidator : AbstractValidator<Create{{entity}}Command>
{
    public Create{{entity}}CommandValidator()
    {
{{body}}
    }
}
""";
	}

	public static string UpdateValidator(string module, string entity, List<EntityField> fields)
	{
		var rules = new List<string>
		{
			"        RuleFor(x => x.Request.Id).GreaterThan(0).WithMessage(\"Geçersiz Id.\");"
		};
		rules.AddRange(fields.Where(f => f.RequiredString)
			.Select(f => $"        RuleFor(x => x.Request.{f.Name}).NotEmpty().WithMessage(\"{f.Name} boş olamaz.\");"));

		var body = string.Join("\n", rules);
		return
$$"""
using {{Feature(module, entity)}}.Commands;
using FluentValidation;

namespace {{Feature(module, entity)}}.Validators;

public class Update{{entity}}CommandValidator : AbstractValidator<Update{{entity}}Command>
{
    public Update{{entity}}CommandValidator()
    {
{{body}}
    }
}
""";
	}

	// ---------- Application / Queries ----------

	public static string GetByIdQuery(string module, string entity) =>
$$"""
using {{Feature(module, entity)}}.Models;
using MediatR;

namespace {{Feature(module, entity)}}.Queries;

public class Get{{entity}}ByIdQuery : IRequest<{{entity}}Response>
{
    public long Id { get; }

    public Get{{entity}}ByIdQuery(long id)
    {
        Id = id;
    }
}
""";

	public static string GetByIdQueryHandler(string module, string entity) =>
$$"""
using {{Feature(module, entity)}}.Converters;
using {{Feature(module, entity)}}.Models;
using {{module}}.Domain.Entities;
using Base.Domain.Interfaces;
using MediatR;

namespace {{Feature(module, entity)}}.Queries;

public class Get{{entity}}ByIdQueryHandler : IRequestHandler<Get{{entity}}ByIdQuery, {{entity}}Response>
{
    private readonly IUnitOfWork _unitOfWork;

    public Get{{entity}}ByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<{{entity}}Response> Handle(Get{{entity}}ByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _unitOfWork.Repository<{{entity}}>().GetByIdAsync(request.Id);

        if (entity is null || !entity.IsActive)
            throw new KeyNotFoundException("{{entity}} bulunamadı.");

        return {{entity}}Converters.{{entity}}Converter(entity);
    }
}
""";

	public static string GetListQuery(string module, string entity) =>
$$"""
using {{Feature(module, entity)}}.Models;
using Base.Application.Common.Models;
using MediatR;

namespace {{Feature(module, entity)}}.Queries;

public class Get{{entity}}ListQuery : IRequest<PagedResult<{{entity}}Response>>
{
    public Get{{entity}}Request Request { get; }

    public Get{{entity}}ListQuery(Get{{entity}}Request request)
    {
        Request = request;
    }
}
""";

	public static string GetListQueryHandler(string module, string entity, List<EntityField> fields)
	{
		var predicates = FilterPredicates(fields);
		return
$$"""
using {{Feature(module, entity)}}.Converters;
using {{Feature(module, entity)}}.Models;
using {{module}}.Domain.Entities;
using Base.Application.Common.Extensions;
using Base.Application.Common.Models;
using Base.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace {{Feature(module, entity)}}.Queries;

public class Get{{entity}}ListQueryHandler : IRequestHandler<Get{{entity}}ListQuery, PagedResult<{{entity}}Response>>
{
    private readonly IUnitOfWork _unitOfWork;

    public Get{{entity}}ListQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<{{entity}}Response>> Handle(Get{{entity}}ListQuery request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        Expression<Func<{{entity}}, bool>> filter = x => x.IsActive;
{{predicates}}

        var (items, total) = await _unitOfWork.Repository<{{entity}}>()
            .GetPagedAsync(req.PageNumber, req.PageSize, filter);

        return new PagedResult<{{entity}}Response>
        {
            Items = {{entity}}Converters.{{entity}}ConverterList(items),
            PageNumber = req.PageNumber,
            PageSize = req.PageSize,
            TotalCount = total
        };
    }
}
""";
	}

	// ---------- Presentation / Controller ----------

	public static string Controller(string module, string entity)
	{
		var route = entity.ToLowerInvariant() + "s";
		return
$$"""
using {{Feature(module, entity)}}.Commands;
using {{Feature(module, entity)}}.Models;
using {{Feature(module, entity)}}.Queries;
using Base.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Controllers.Base;

namespace Presentation.Controllers.{{module}};

[ApiController]
[Route("api/{{route}}")]
public class {{entity}}Controller : ApiControllerBase
{
    /// <summary>Sayfalı + filtreli liste (anonim).</summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] Get{{entity}}Request request)
    {
        var result = await Mediator.Send(new Get{{entity}}ListQuery(request));
        return Ok(result);
    }

    /// <summary>Tekil kayıt (anonim).</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await Mediator.Send(new Get{{entity}}ByIdQuery(id));
        return Ok(result);
    }

    /// <summary>Yeni kayıt (yetki gerekir).</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] Create{{entity}}Request request)
    {
        var result = await Mediator.Send(new Create{{entity}}Command(request));
        return Ok(result);
    }

    /// <summary>Güncelle (yetki gerekir).</summary>
    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Update(long id, [FromBody] Update{{entity}}Request request)
    {
        request.Id = id;
        var result = await Mediator.Send(new Update{{entity}}Command(request));
        return Ok(result);
    }

    /// <summary>Sil (soft delete, yetki gerekir).</summary>
    [HttpDelete("{id:long}")]
    [Authorize]
    public async Task<IActionResult> Delete(long id)
    {
        await Mediator.Send(new Delete{{entity}}Command(id));
        return Ok();
    }
}
""";
	}
}
