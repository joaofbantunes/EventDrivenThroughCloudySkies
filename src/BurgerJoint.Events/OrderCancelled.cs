namespace BurgerJoint.Events
{
    public record OrderCancelled : OrderEventBase
    {
        public string Reason { get; init; }
    }
}