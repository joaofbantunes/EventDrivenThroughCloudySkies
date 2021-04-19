using System;

namespace BurgerJoint.Events
{
    public abstract record OrderEventBase
    {
        public Guid Id { get; init; }
        
        public Guid OrderId { get; init; }

        public Guid DishId { get; init; }

        public string CustomerNumber { get; init; }
        
        public DateTime OccurredAt { get; init; }
    }
}