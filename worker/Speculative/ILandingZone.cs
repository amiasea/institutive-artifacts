public interface ILandingZone
{
    string Id { get; }

    // Task<Environment> AddEnvironmentAsync(
    //     CancellationToken cancellationToken = default);

    Task RemoveEnvironmentAsync(
        Environment environment,
        CancellationToken cancellationToken = default);

    Task VacateAsync(
        Environment environment,
        CancellationToken cancellationToken = default);

    // IList<Environment> Environments { get; }
}