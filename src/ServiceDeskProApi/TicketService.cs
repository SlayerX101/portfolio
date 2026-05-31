namespace ServiceDeskProApi;

public sealed class TicketService(TicketStore store)
{
    public IReadOnlyCollection<TicketSummary> Search(TicketQuery query)
    {
        var tickets = store.Tickets.AsEnumerable();

        if (TryParseEnum(query.Status, out TicketStatus status))
        {
            tickets = tickets.Where(ticket => ticket.Status == status);
        }

        if (TryParseEnum(query.Priority, out TicketPriority priority))
        {
            tickets = tickets.Where(ticket => ticket.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(query.AssignedTo))
        {
            tickets = tickets.Where(ticket => ticket.AssignedTo?.Contains(query.AssignedTo, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            tickets = tickets.Where(ticket =>
                ticket.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                ticket.Description.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                ticket.CustomerName.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Breached is not null)
        {
            tickets = tickets.Where(ticket => IsSlaBreached(ticket) == query.Breached);
        }

        return tickets
            .OrderByDescending(ticket => ticket.Priority)
            .ThenBy(ticket => ticket.SlaDueAt)
            .Select(ToSummary)
            .ToList();
    }

    public SupportTicket? Get(int id) => store.Tickets.FirstOrDefault(ticket => ticket.Id == id);

    public OperationResult<SupportTicket> Create(CreateTicketRequest request)
    {
        var errors = ValidateCreateRequest(request);
        if (errors.Count > 0)
        {
            return OperationResult<SupportTicket>.Failure([.. errors]);
        }

        var priority = Enum.Parse<TicketPriority>(request.Priority, true);
        var now = DateTimeOffset.UtcNow;
        var ticket = new SupportTicket(
            store.NextTicketId(),
            request.Title.Trim(),
            request.Description.Trim(),
            request.CustomerName.Trim(),
            request.CustomerEmail.Trim(),
            priority,
            TicketStatus.New,
            null,
            now,
            now.Add(CalculateSla(priority)),
            null,
            [],
            [CreateAudit("Created", "system", $"Ticket opened for {request.CustomerName.Trim()}."),
             CreateAudit("SlaCalculated", "system", $"{priority} SLA due at {now.Add(CalculateSla(priority)):u}.")]);

        return OperationResult<SupportTicket>.Success(store.Add(ticket));
    }

    public OperationResult<SupportTicket> Assign(int id, AssignTicketRequest request)
    {
        var ticket = Get(id);
        if (ticket is null)
        {
            return OperationResult<SupportTicket>.Missing("Ticket not found.");
        }

        if (string.IsNullOrWhiteSpace(request.AgentName) || string.IsNullOrWhiteSpace(request.Actor))
        {
            return OperationResult<SupportTicket>.Failure("Agent name and actor are required.");
        }

        ticket.AuditTrail.Add(CreateAudit("Assigned", request.Actor.Trim(), $"Assigned to {request.AgentName.Trim()}."));
        Replace(ticket, ticket with
        {
            AssignedTo = request.AgentName.Trim(),
            Status = ticket.Status == TicketStatus.New ? TicketStatus.Assigned : ticket.Status
        });

        return OperationResult<SupportTicket>.Success(Get(id)!);
    }

    public OperationResult<SupportTicket> UpdateStatus(int id, UpdateTicketStatusRequest request)
    {
        var ticket = Get(id);
        if (ticket is null)
        {
            return OperationResult<SupportTicket>.Missing("Ticket not found.");
        }

        if (!TryParseEnum(request.Status, out TicketStatus status))
        {
            return OperationResult<SupportTicket>.Failure("Status is invalid.");
        }

        if (!CanTransition(ticket.Status, status))
        {
            return OperationResult<SupportTicket>.Failure($"Cannot transition from {ticket.Status} to {status}.");
        }

        if ((status is TicketStatus.Resolved or TicketStatus.Closed) && string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            return OperationResult<SupportTicket>.Failure("Resolution note is required when resolving or closing a ticket.");
        }

        ticket.AuditTrail.Add(CreateAudit("StatusChanged", request.Actor, $"{ticket.Status} -> {status}."));
        if (!string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            ticket.Comments.Add(new TicketComment(store.NextCommentId(), request.Actor, request.ResolutionNote.Trim(), DateTimeOffset.UtcNow, true));
        }

        Replace(ticket, ticket with
        {
            Status = status,
            ResolvedAt = status is TicketStatus.Resolved or TicketStatus.Closed ? DateTimeOffset.UtcNow : ticket.ResolvedAt
        });

        return OperationResult<SupportTicket>.Success(Get(id)!);
    }

    public OperationResult<SupportTicket> AddComment(int id, AddTicketCommentRequest request)
    {
        var ticket = Get(id);
        if (ticket is null)
        {
            return OperationResult<SupportTicket>.Missing("Ticket not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Author) || string.IsNullOrWhiteSpace(request.Message))
        {
            return OperationResult<SupportTicket>.Failure("Author and message are required.");
        }

        ticket.Comments.Add(new TicketComment(store.NextCommentId(), request.Author.Trim(), request.Message.Trim(), DateTimeOffset.UtcNow, request.IsInternal));
        ticket.AuditTrail.Add(CreateAudit("CommentAdded", request.Author.Trim(), request.IsInternal ? "Internal note added." : "Customer-visible comment added."));
        return OperationResult<SupportTicket>.Success(ticket);
    }

    public OperationResult<IReadOnlyCollection<TicketAuditEvent>> GetAuditTrail(int id)
    {
        var ticket = Get(id);
        return ticket is null
            ? OperationResult<IReadOnlyCollection<TicketAuditEvent>>.Missing("Ticket not found.")
            : OperationResult<IReadOnlyCollection<TicketAuditEvent>>.Success(ticket.AuditTrail.OrderBy(audit => audit.CreatedAt).ToList());
    }

    public DashboardMetric GetDashboard()
    {
        var tickets = store.Tickets;
        var resolved = tickets.Where(ticket => ticket.ResolvedAt is not null).ToList();
        var averageResolutionHours = resolved.Count == 0
            ? 0
            : resolved.Average(ticket => (ticket.ResolvedAt!.Value - ticket.CreatedAt).TotalHours);

        return new DashboardMetric(
            tickets.Count,
            tickets.Count(ticket => ticket.Status is not TicketStatus.Resolved and not TicketStatus.Closed),
            tickets.Count(IsSlaBreached),
            Math.Round(averageResolutionHours, 2),
            tickets.GroupBy(ticket => ticket.Status).Select(group => new StatusCount(group.Key.ToString(), group.Count())).ToList(),
            tickets.GroupBy(ticket => ticket.Priority).Select(group => new PriorityCount(group.Key.ToString(), group.Count())).ToList(),
            tickets.Where(ticket => ticket.Status is not TicketStatus.Resolved and not TicketStatus.Closed && ticket.AssignedTo is not null)
                .GroupBy(ticket => ticket.AssignedTo!)
                .Select(group => new AgentLoad(group.Key, group.Count()))
                .ToList());
    }

    private static TicketSummary ToSummary(SupportTicket ticket) =>
        new(
            ticket.Id,
            ticket.Title,
            ticket.CustomerName,
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.AssignedTo,
            ticket.CreatedAt,
            ticket.SlaDueAt,
            IsSlaBreached(ticket),
            ticket.Comments.Count);

    private static bool IsSlaBreached(SupportTicket ticket) =>
        ticket.Status is not TicketStatus.Resolved and not TicketStatus.Closed &&
        DateTimeOffset.UtcNow > ticket.SlaDueAt;

    private TicketAuditEvent CreateAudit(string action, string actor, string details) =>
        new(store.NextAuditId(), action, string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim(), details, DateTimeOffset.UtcNow);

    private static TimeSpan CalculateSla(TicketPriority priority) =>
        priority switch
        {
            TicketPriority.Critical => TimeSpan.FromHours(4),
            TicketPriority.High => TimeSpan.FromHours(12),
            TicketPriority.Medium => TimeSpan.FromHours(48),
            _ => TimeSpan.FromDays(5)
        };

    private static bool CanTransition(TicketStatus from, TicketStatus to) =>
        from == to ||
        (from, to) is
        (TicketStatus.New, TicketStatus.Assigned) or
        (TicketStatus.New, TicketStatus.InProgress) or
        (TicketStatus.Assigned, TicketStatus.InProgress) or
        (TicketStatus.InProgress, TicketStatus.WaitingForCustomer) or
        (TicketStatus.WaitingForCustomer, TicketStatus.InProgress) or
        (TicketStatus.InProgress, TicketStatus.Resolved) or
        (TicketStatus.WaitingForCustomer, TicketStatus.Resolved) or
        (TicketStatus.Resolved, TicketStatus.Closed);

    private static List<string> ValidateCreateRequest(CreateTicketRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors.Add("Description is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            errors.Add("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || !request.CustomerEmail.Contains('@'))
        {
            errors.Add("A valid customer email is required.");
        }

        if (!TryParseEnum(request.Priority, out TicketPriority _))
        {
            errors.Add("Priority must be Low, Medium, High, or Critical.");
        }

        return errors;
    }

    private static bool TryParseEnum<T>(string? value, out T parsed)
        where T : struct, Enum =>
        Enum.TryParse(value, true, out parsed);

    private void Replace(SupportTicket current, SupportTicket updated)
    {
        var list = (List<SupportTicket>)store.Tickets;
        var index = list.FindIndex(ticket => ticket.Id == current.Id);
        list[index] = updated;
    }
}
