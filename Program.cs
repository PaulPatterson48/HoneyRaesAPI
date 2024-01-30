using HoneyRaesAPI.Models;

List<Customer> customers = new List<Customer>
{
    new Customer() {Id=0, Name="John Doe", Address="123 Main St Nashville, TN 37011"},
    new Customer() {Id=1, Name="John Smith", Address="234 Fessler Lane Nashville, TN 37012"},
    new Customer() {Id=2, Name="Jane Jones", Address="535 Bell Road Antioch TN 37013"}
};
List<Employee> employees = new List<Employee>
{
    new Employee() {Id=0, Name="Sandy Monroe", Specialty="Internal Combustion"},
    new Employee() {Id=1, Name ="Mike Smith", Specialty="Electrical"}
};
List<ServiceTicket> serviceTickets = new List<ServiceTicket>
{
    new ServiceTicket() {Id = 0, CustomerId = 0, EmployeeId = 0, Description = "Car is rattling", Emergency = false, DateCompleted ="02/02/2023"},
    new ServiceTicket() {Id = 1, CustomerId = 1, EmployeeId = 1, Description = "Battery won't charge", Emergency = true, DateCompleted= "05/03/2023" },
    new ServiceTicket() {Id = 2, CustomerId = 2, EmployeeId = 0, Description = "Break light out", Emergency= false, DateCompleted= "07/11/2023"},
    new ServiceTicket() {Id = 3, CustomerId = 0, EmployeeId = 1, Description = "Engine light on", Emergency = false, DateCompleted = "09/12/2023"},
    new ServiceTicket() {Id = 4, CustomerId = 1, EmployeeId = 0, Description = "Dashboard lights out", Emergency = true, DateCompleted = "01/15/2024"}
};


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/employees/{id}", (int id) =>
{
    Employee employee = employees.FirstOrDefault(e => e.Id == id);
    employee.ServiceTickets = serviceTickets.Where(st => st.EmployeeId == id).ToList();
    if (employee == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(employee);
} );

app.MapGet("/customer/{id}", (int id) =>
{
    Customer customer = customers.FirstOrDefault(c => c.Id == id);
    if (customer == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(customer);
});

app.MapGet("/servicetickets/{id}", (int id) =>
{
    ServiceTicket serviceTicket = serviceTickets.FirstOrDefault(st => st.Id == id);
    if (serviceTicket == null)
{
        return Results.NotFound();
}
    serviceTicket.Employee = employees.FirstOrDefault(e => e.Id == serviceTicket.EmployeeId);
    return Results.Ok(serviceTicket);
});

app.MapPost("/servicetickets", (ServiceTicket serviceTicket) =>
{
    //creates a new id (When we get to it later, our SQL database will do this for us like JSON Server did!)
    serviceTicket.Id = serviceTickets.Max(st => st.Id) + 1;
    serviceTickets.Add(serviceTicket);
    return serviceTicket;
});

app.MapDelete("/servicetickets/{id}", (int id) =>
{
    // Find the service ticket with the specified id
    ServiceTicket serviceTicketToDelete = serviceTickets.FirstOrDefault(st => st.Id == id);

    // If not found, return 404 Not Found
    if (serviceTicketToDelete == null)
    {
        return Results.NotFound();
    }

    // Remove the service ticket from the list
    serviceTickets.Remove(serviceTicketToDelete);

    // Return success with no content (204 No Content)
    return Results.NoContent();
});

app.MapPut("/servicetickets/{id}", (int id, ServiceTicket serviceTicket) =>
{
    ServiceTicket ticketToUpdate = serviceTickets.FirstOrDefault(st => st.Id == id);
    int ticketIndex = serviceTickets.IndexOf(ticketToUpdate);
    if (ticketToUpdate == null)
    {
        return Results.NotFound();
    }
    //the id in the request route doesn't match the id from the ticket in the request body. That's a bad request!
    if (id != serviceTicket.Id)
    {
        return Results.BadRequest();
    }
    serviceTickets[ticketIndex] = serviceTicket;
    return Results.Ok();
});

app.MapPost("/servicetickets/{id}/complete", (int id) =>
{
    ServiceTicket ticketToComplete = serviceTickets.FirstOrDefault(st => st.Id == id);
    ticketToComplete.DateCompleted = DateTime.Today.ToString();
});

app.MapGet("/incompleteEmergencyServiceTickets", () =>
{
    List<ServiceTicket> incompleteEmergencyTickets = serviceTickets
    .Where(st => !st.Emergency && string.IsNullOrEmpty(st.DateCompleted)).ToList();

    return Results.Ok(incompleteEmergencyTickets);
});

app.MapGet("/unassignedServiceTickets", () =>
{
    List<ServiceTicket> unassignedServiceTickets = serviceTickets
    .Where(st => st.EmployeeId == null).ToList();

    return Results.Ok(unassignedServiceTickets);
});

app.MapGet("/noClosedServiceTicketForAYear", () =>
{
    DateTime currentDate = DateTime.Now;
    int oneYearAgo = currentDate.Year - 1; //Get last year this time

    List<Customer> customersWithoutClosedServiceTicketForYear = customers
        .Where(c =>
        {
            List<ServiceTicket> customerServiceTickets = serviceTickets
                .Where(st => st.CustomerId == c.Id && !string.IsNullOrEmpty(st.DateCompleted))
                .ToList();

            if (customerServiceTickets.Count == 0)
                return true;

            DateTime lastClosedDate = customerServiceTickets.Max(st => DateTime.Parse(st.DateCompleted));
            int yearsSinceLastClosed = currentDate.Year - lastClosedDate.Year; //determined how many days the ticket is open

            return yearsSinceLastClosed > 1; //Give me all tickets that meet the criteria
        })
        .ToList();

    return Results.Ok(customersWithoutClosedServiceTicketForYear);
});

app.MapGet("/openEmployees", () =>
{
    List<Employee> openEmployees = employees
    .Where(e => !serviceTickets.Any(st => st.EmployeeId == e.Id
    && string.IsNullOrEmpty(st.DateCompleted))).ToList();

    return Results.Ok(openEmployees);
});

app.MapGet("/employeesForCustomers/{employeeId}",(int employeeId) =>
{
    List<int> employeesAssginedToCustomers = serviceTickets
    .Where(st => st.EmployeeId == employeeId)
    .Select(c => c.CustomerId).Distinct().ToList();

    List<Customer> employeesForCustomers = customers
    .Where(c => employeesAssginedToCustomers.Contains(c.Id)).ToList();

    return Results.Ok(employeesForCustomers);
});

app.MapGet("/customersForEmployee/{employeeId}", (int employeeId) =>
{
    Employee employee = employees.FirstOrDefault(e => e.Id == employeeId);

    if (employee == null)
    {
        return Results.NotFound();
    }

    List<int> customerIdsForEmployee = employee.ServiceTickets
        .Where(st => st.EmployeeId == employeeId)
        .Select(st => st.CustomerId)
        .Distinct()
        .ToList();

    List<Customer> customersForEmployee = customers
        .Where(c => customerIdsForEmployee.Contains(c.Id))
        .ToList();

    return Results.Ok(customersForEmployee);
});

app.MapGet("/mostCompletedLastMonth", () =>
{
    //Get date for previous month
    DateTime lastMonth = DateTime.Now.AddMonths(-1);

    var empMostCompletedPrevMonth = serviceTickets
    .Where(st => !string.IsNullOrEmpty(st.DateCompleted)
    && DateTime.Parse(st.DateCompleted) >= lastMonth)
    .GroupBy(st => st.EmployeeId).Select(group => new
    {
        EmployeeId = group.Key,
        ticketCount = group.Count()
    }).OrderByDescending(x => x.ticketCount).FirstOrDefault();

    if (empMostCompletedPrevMonth == null)
    {
        return Results.NotFound();
    }
    Employee employee = employees.FirstOrDefault(e => e.Id
    == empMostCompletedPrevMonth.EmployeeId);

    return Results.Ok(employee);
});

app.MapGet("/completedTicketsDecending", () =>
{
    List<ServiceTicket> completedTickets = serviceTickets
    .Where(st => !string.IsNullOrEmpty(st.DateCompleted)).ToList();

    return Results.Ok(completedTickets);
});

app.Run();


