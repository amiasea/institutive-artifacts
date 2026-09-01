namespace Amiasea.Speculative;

public interface IBooking
{
  bool HasVacancy { get; }
  // Task AddReservationRequestAsync(
  //     ReservationRequest request,
  //     CancellationToken cancellationToken = default);

  // Task AddReleaseRequestAsync(
  //     ReleaseRequest request,
  //     CancellationToken cancellationToken = default);

  Task CreateBookingAsync();

  // Task<Ticket> CreateTicket();

  // Task<Ticket> UpdateTicket();

  // Final Validation
}