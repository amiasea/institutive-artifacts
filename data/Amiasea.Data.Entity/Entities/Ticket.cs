// Use this to collect information for a Reservation or a Confirmation

namespace Amiasea.Data.Entities.Speculative;

public class Ticket
{
    public IDictionary<TicketClaimsEnum, string> Claims { get; set; } = new Dictionary<TicketClaimsEnum, string>();
}