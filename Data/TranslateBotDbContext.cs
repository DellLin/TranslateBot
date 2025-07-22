using Microsoft.EntityFrameworkCore;
using TranslateBot.Models;
using Pgvector.EntityFrameworkCore;

namespace TranslateBot.Data;

public class TranslateBotDbContext : DbContext
{
    public TranslateBotDbContext(DbContextOptions<TranslateBotDbContext> options) : base(options) { }

    public DbSet<AddressReference> AddressReferences { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // UseVector 會在 Program.cs 中配置
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置 AddressReference 表
        modelBuilder.Entity<AddressReference>(entity =>
        {
            entity.ToTable("address_references");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.Type)
                .HasColumnName("type")
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Embedding)
                .HasColumnName("embedding")
                .HasColumnType("vector(768)"); // 假設使用 768 維 embedding

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            // 為向量相似性搜索創建索引
            entity.HasIndex(e => e.Embedding)
                .HasDatabaseName("ix_address_references_embedding");
        });
    }
}
