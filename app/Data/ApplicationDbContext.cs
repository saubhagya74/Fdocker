using app.Models;
using Microsoft.EntityFrameworkCore;

namespace app.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<MessageEntity> Messages { get; set; }
        public DbSet<FriendEntity> Friends { get; set; }
        public DbSet<FriendRequestEntity> FriendRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MessageEntity>(entity =>
            {
                entity.HasKey(m => m.MessageId);

                entity.HasOne<UserEntity>()
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<UserEntity>()
                    .WithMany()
                    .HasForeignKey(m => m.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });//doing foreign key
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FriendEntity>(entity =>
            {
                entity.HasKey(m => m.FriendId);

                entity.HasOne<UserEntity>()
                    .WithMany()
                    .HasForeignKey(m => m.FUserId1)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<UserEntity>()
                    .WithMany()
                    .HasForeignKey(m => m.FUserId2)
                    .OnDelete(DeleteBehavior.Restrict);
            });//doing foreign key
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FriendRequestEntity>(entity =>
            {
                entity.HasKey(m => m.RequestId);

                entity.HasOne<UserEntity>()
                    .WithMany()
                    .HasForeignKey(m => m.RequesterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<UserEntity>()
                    .WithMany()
                    .HasForeignKey(m => m.RequestToId)
                    .OnDelete(DeleteBehavior.Restrict);
            });//doing foreign key
        }
        }
}
