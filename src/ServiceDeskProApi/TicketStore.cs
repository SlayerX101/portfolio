namespace ServiceDeskProApi;

public sealed class TicketStore
{
    private readonly List<SupportTicket> tickets;
    private int nextTicketId = 4;
    private int nextCommentId = 3;
    private int nextAuditId = 7;

    public TicketStore()
    {
        var now = DateTimeOffset.UtcNow;
        tickets =
        [
            new SupportTicket(
                1,
                "Cannot access booking dashboard",
                "User receives an access denied message after signing in.",
                "Amina Jacobs",
                "amina@example.com",
                TicketPriority.High,
                TicketStatus.InProgress,
                "Oageng",
                now.AddHours(-9),
                now.AddHours(3),
                null,
                [new TicketComment(1, "Oageng", "Confirmed the issue is role-related.", now.AddHours(-7), true)],
                [new TicketAuditEvent(1, "Created", "system", "Ticket created from customer request.", now.AddHours(-9)),
                 new TicketAuditEvent(2, "Assigned", "Oageng", "Assigned to Oageng.", now.AddHours(-8))]),
            new SupportTicket(
                2,
                "Invoice export missing customer column",
                "Finance team needs customer name included in spreadsheet exports.",
                "Sipho Mokoena",
                "sipho@example.com",
                TicketPriority.Medium,
                TicketStatus.Assigned,
                "Naledi",
                now.AddHours(-18),
                now.AddHours(30),
                null,
                [],
                [new TicketAuditEvent(3, "Created", "system", "Ticket created from email.", now.AddHours(-18)),
                 new TicketAuditEvent(4, "Assigned", "Naledi", "Assigned to Naledi.", now.AddHours(-16))]),
            new SupportTicket(
                3,
                "Payment callback failure",
                "Payment provider callback fails intermittently for online bookings.",
                "Dineo Nkosi",
                "dineo@example.com",
                TicketPriority.Critical,
                TicketStatus.New,
                null,
                now.AddHours(-2),
                now.AddHours(2),
                null,
                [new TicketComment(2, "Dineo", "Issue affects live payments.", now.AddHours(-2), false)],
                [new TicketAuditEvent(5, "Created", "system", "Critical ticket created.", now.AddHours(-2)),
                 new TicketAuditEvent(6, "SlaCalculated", "system", "Critical SLA set to 4 hours.", now.AddHours(-2))])
        ];
    }

    public IReadOnlyCollection<SupportTicket> Tickets => tickets;

    public SupportTicket Add(SupportTicket ticket)
    {
        tickets.Add(ticket);
        return ticket;
    }

    public int NextTicketId() => nextTicketId++;
    public int NextCommentId() => nextCommentId++;
    public int NextAuditId() => nextAuditId++;
}
