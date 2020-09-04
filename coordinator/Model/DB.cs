using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Karenia.Rurikawa.Helpers;
using Karenia.Rurikawa.Models.Account;
using Karenia.Rurikawa.Models.Judger;
using Karenia.Rurikawa.Models.Test;
using Marques.EFCore.SnakeCase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#pragma warning disable CS8618
namespace Karenia.Rurikawa.Models {
    public class RurikawaDb : DbContext {
        public RurikawaDb(DbContextOptions ctx) : base(ctx) {
        }


        /// <summary>
        /// All jobs added to this judging system
        /// </summary>
        public DbSet<Job> Jobs { get; set; }

        /// <summary>
        /// All judgers added to this system
        /// </summary>
        public DbSet<JudgerEntry> Judgers { get; set; }

        public DbSet<TestSuite> TestSuites { get; set; }

        public DbSet<Account.UserAccount> Accounts { get; set; }

        public DbSet<Account.Profile> Profiles { get; set; }

        public DbSet<Account.TokenEntry> AccessTokens { get; set; }

        public DbSet<Account.TokenEntry> RefreshTokens { get; set; }

        public DbSet<Account.TokenEntry> JudgerRegisterTokens { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder opt) {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            var flowSnakeConverter = new ValueConverter<FlowSnake, long>(
                x => x,
                x => new FlowSnake(x));

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Job>().HasKey(x => x.Id);
            modelBuilder.Entity<TestSuite>().HasKey(x => x.Id);
            modelBuilder.Entity<JudgerEntry>().HasKey(x => x.Id);
            modelBuilder.Entity<UserAccount>().HasKey(x => x.Username);
            modelBuilder.Entity<Profile>().HasKey(x => x.Username);
            modelBuilder.Entity<TokenEntry>().HasKey(x => x.Token);

            modelBuilder.Entity<Job>().HasIndex(x => x.Id).IsUnique();
            modelBuilder.Entity<Job>().HasIndex(x => x.Account);
            modelBuilder.Entity<Job>().HasIndex(x => x.TestSuite);
            modelBuilder.Entity<JudgerEntry>().HasIndex(x => x.Id).IsUnique();
            modelBuilder.Entity<JudgerEntry>().HasIndex(x => x.Tags);
            modelBuilder.Entity<UserAccount>().HasIndex(x => x.Username).IsUnique();
            modelBuilder.Entity<UserAccount>().HasIndex(x => x.Kind);
            modelBuilder.Entity<Profile>().HasIndex(x => x.Username).IsUnique();
            modelBuilder.Entity<Profile>().HasIndex(x => x.Email);
            modelBuilder.Entity<TokenEntry>().HasIndex(x => x.Token).IsUnique();
            modelBuilder.Entity<TokenEntry>().HasIndex(x => x.Expires);
            modelBuilder.Entity<TokenEntry>().HasIndex(x => x.Username);
            modelBuilder.Entity<TokenEntry>().HasIndex(x => x.TokenName);
            modelBuilder.Entity<TestSuite>().HasIndex(x => x.Name).IsUnique();
            modelBuilder.Entity<TestSuite>().HasIndex(x => x.Id).IsUnique();

            modelBuilder.Entity<Job>().Property(x => x.Id)
                .HasConversion(flowSnakeConverter);
            modelBuilder.Entity<Job>().Property(x => x.TestSuite)
                .HasConversion(flowSnakeConverter);
            modelBuilder.Entity<TestSuite>().Property(x => x.Id)
                .HasConversion(flowSnakeConverter);

            modelBuilder.ToSnakeCase();
        }
    }
}
