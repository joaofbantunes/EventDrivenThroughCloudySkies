using BurgerJoint.StoreFront.Data;
using BurgerJoint.StoreFront.Data.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace BurgerJoint.StoreFront.Infrastructure.Data.Configurations
{
    public class OutboxMessageConfiguration: IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            
            builder.HasKey(e => e.Id);
            builder
                .Property(e => e.OrderEventBase)
                .HasColumnType("nvarchar(max)") 
                .HasConversion(
                    e => JsonConvert.SerializeObject(e, settings),
                    e => JsonConvert.DeserializeObject<OrderEventBase>(e, settings));
        }
    }
}