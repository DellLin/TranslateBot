using Microsoft.EntityFrameworkCore;
using TranslateBot.Models;
using Pgvector.EntityFrameworkCore;

namespace TranslateBot.Data;

public class TranslateBotDbContext : DbContext
{
    public TranslateBotDbContext(DbContextOptions<TranslateBotDbContext> options) : base(options) { }

    public DbSet<AddressReference> AddressReferences { get; set; }
    public DbSet<TranslationRecord> TranslationRecords { get; set; }

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

        // 配置 TranslationRecord 表
        modelBuilder.Entity<TranslationRecord>(entity =>
        {
            entity.ToTable("translation_records");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.OriginalText)
                .HasColumnName("original_text")
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.TranslatedText)
                .HasColumnName("translated_text")
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.TranslationType)
                .HasColumnName("translation_type")
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.UserSession)
                .HasColumnName("user_session")
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.ProcessingTime)
                .HasColumnName("processing_time");

            entity.Property(e => e.IsSuccessful)
                .HasColumnName("is_successful")
                .HasDefaultValue(true);

            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message")
                .HasMaxLength(1000);

            // 為常用查詢建立索引
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("ix_translation_records_created_at");

            entity.HasIndex(e => e.TranslationType)
                .HasDatabaseName("ix_translation_records_type");

            entity.HasIndex(e => e.UserSession)
                .HasDatabaseName("ix_translation_records_session");
        });
    }
}
