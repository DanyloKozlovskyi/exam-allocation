using Domain.Centers;
using Domain.ValueObjects;
using FluentValidation;

namespace Application.Centers.Commands;

public class UpdateTestCenterCommandValidator : AbstractValidator<UpdateTestCenterCommand>
{
    public UpdateTestCenterCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(TestCenterConstraints.NameMaxLength);
        RuleFor(x => x.Address).MaximumLength(TestCenterConstraints.AddressMaxLength);
        RuleFor(x => x.Latitude).InclusiveBetween(GeoPoint.MinLatitude, GeoPoint.MaxLatitude);
        RuleFor(x => x.Longitude).InclusiveBetween(GeoPoint.MinLongitude, GeoPoint.MaxLongitude);
        RuleFor(x => x.GooglePlaceId).MaximumLength(TestCenterConstraints.GooglePlaceIdMaxLength);
        RuleFor(x => x.CostPerRoom).GreaterThanOrEqualTo(TestCenterConstraints.MinCostPerRoom);
    }
}