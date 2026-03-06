using FluentValidation;
using TodoApp.Api.Contracts.Todos;

namespace TodoApp.Api.Validation;

public sealed class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.DueAtUtc)
            .Must(due => due is null || due > DateTime.UtcNow.AddMinutes(-1))
            .WithMessage("DueAtUtc must be in the future.");
    }
}