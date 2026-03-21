using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Forum> Forums { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Reply> Replies { get; set; }
        public DbSet<Attachment> Attachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configurations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Forum configurations
            modelBuilder.Entity<Forum>(entity =>
            {
                entity.HasOne(f => f.CreatedBy)
                    .WithMany()
                    .HasForeignKey(f => f.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TaskItem configurations
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasOne(t => t.Forum)
                    .WithMany(f => f.Tasks)
                    .HasForeignKey(t => t.ForumId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.CreatedBy)
                    .WithMany(u => u.CreatedTasks)
                    .HasForeignKey(t => t.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.AssignedTo)
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(t => t.AssignedToUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.Priority);
                entity.HasIndex(t => t.CreatedAt);
            });

            // Reply configurations
            modelBuilder.Entity<Reply>(entity =>
            {
                entity.HasOne(r => r.Task)
                    .WithMany(t => t.Replies)
                    .HasForeignKey(r => r.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                    .WithMany(u => u.Replies)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Attachment configurations
            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.HasOne(a => a.Task)
                    .WithMany(t => t.Attachments)
                    .HasForeignKey(a => a.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Reply)
                    .WithMany(r => r.Attachments)
                    .HasForeignKey(a => a.ReplyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
