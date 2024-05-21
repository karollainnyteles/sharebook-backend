using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShareBook.Domain;

namespace ShareBook.Repository.Mapping
{
    public class AddressMap
    {
        private const string Varchar50Type = "varchar(50)";

        public AddressMap(EntityTypeBuilder<Address> entityBuilder)
        {
            entityBuilder.Property(t => t.PostalCode)
                   .HasColumnType("varchar(15)")
                   .HasMaxLength(15);

            entityBuilder.Property(t => t.Neighborhood)
                  .HasColumnType(Varchar50Type)
                  .HasMaxLength(30);

            entityBuilder.Property(t => t.Complement)
                  .HasColumnType(Varchar50Type)
                  .HasMaxLength(30);

            entityBuilder.Property(t => t.Country)
                  .HasColumnType(Varchar50Type)
                  .HasMaxLength(30);

            entityBuilder.Property(t => t.City)
                .HasColumnType(Varchar50Type)
                .HasMaxLength(30);

            entityBuilder.Property(t => t.State)
                .HasColumnType("varchar(30)")
                .HasMaxLength(30);

            entityBuilder.Property(t => t.Street)
               .HasColumnType("varchar(80)")
               .HasMaxLength(50);

            entityBuilder.Property(t => t.Number)
              .HasColumnType("varchar(10)")
              .HasMaxLength(10);
        }
    }
}