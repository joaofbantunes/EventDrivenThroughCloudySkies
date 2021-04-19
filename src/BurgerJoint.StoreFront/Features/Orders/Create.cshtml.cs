using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BurgerJoint.Events;
using BurgerJoint.StoreFront.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BurgerJoint.StoreFront.Features.Orders
{
    public class Create : PageModel
    {
        private readonly BurgerDbContext _db;
        private readonly IOrderEventPublisher _orderEventPublisher;

        public Create(BurgerDbContext db, IOrderEventPublisher orderEventPublisher)
        {
            _db = db;
            _orderEventPublisher = orderEventPublisher;
        }

        public IReadOnlyCollection<SelectListItem> Dishes { get; private set; }

        [BindProperty] public Guid DishId { get; set; }
        [BindProperty] [Display(Name = "Customer Number")]public string CustomerNumber { get; set; }

        public async Task OnGetAsync()
        {
            Dishes = await _db.Dishes
                .AsNoTracking()
                .Select(d => new SelectListItem(d.Name, d.Id.ToString()))
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var dish = await _db.Dishes.FindAsync(DishId);
            var order = Order.Create(dish, CustomerNumber);
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            
            // what if after completing the transaction, this fails?
            await _orderEventPublisher.PublishAsync(
                new OrderCreated
                {
                    Id = Guid.NewGuid(),
                    DishId = order.Dish.Id,
                    OrderId = order.Id,
                    OccurredAt = DateTime.UtcNow,
                    CustomerNumber = CustomerNumber
                }, 
                HttpContext.RequestAborted);
            
            return RedirectToPage(nameof(InProgress));
        }
    }
}