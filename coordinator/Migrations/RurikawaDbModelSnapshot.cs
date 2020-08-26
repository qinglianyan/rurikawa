﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Karenia.Rurikawa.Models;
using Karenia.Rurikawa.Models.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Karenia.Rurikawa.Coordinator.Migrations
{
    [DbContext(typeof(RurikawaDb))]
    partial class RurikawaDbModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Karenia.Rurikawa.Models.Account.Profile", b =>
                {
                    b.Property<string>("Username")
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .HasColumnType("text");

                    b.Property<string>("StudentId")
                        .HasColumnType("text");

                    b.HasKey("Username");

                    b.HasIndex("Email");

                    b.HasIndex("Username");

                    b.ToTable("Profiles");
                });

            modelBuilder.Entity("Karenia.Rurikawa.Models.Account.TokenEntry", b =>
                {
                    b.Property<string>("AccessToken")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset?>("Expires")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("TokenName")
                        .HasColumnType("text");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("AccessToken");

                    b.HasIndex("AccessToken");

                    b.HasIndex("Expires");

                    b.HasIndex("TokenName");

                    b.HasIndex("Username");

                    b.ToTable("TokenEntry");
                });

            modelBuilder.Entity("Karenia.Rurikawa.Models.Account.UserAccount", b =>
                {
                    b.Property<string>("Username")
                        .HasColumnType("text");

                    b.Property<byte[]>("HashedPassword")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.Property<int>("Kind")
                        .HasColumnType("integer");

                    b.Property<byte[]>("Salt")
                        .IsRequired()
                        .HasColumnType("bytea");

                    b.HasKey("Username");

                    b.HasIndex("Kind");

                    b.HasIndex("Username");

                    b.ToTable("Accounts");
                });

            modelBuilder.Entity("Karenia.Rurikawa.Models.Judger.Job", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint");

                    b.Property<string>("Account")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Branch")
                        .HasColumnType("text");

                    b.Property<string>("Repo")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Dictionary<string, TestResult>>("Results")
                        .HasColumnType("jsonb");

                    b.Property<int>("Stage")
                        .HasColumnType("integer");

                    b.Property<long>("TestSuite")
                        .HasColumnType("bigint");

                    b.Property<List<string>>("Tests")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.HasKey("Id");

                    b.HasIndex("Account");

                    b.HasIndex("Id");

                    b.HasIndex("TestSuite");

                    b.ToTable("Jobs");
                });

            modelBuilder.Entity("Karenia.Rurikawa.Models.Judger.JudgerEntry", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<bool>("AcceptUntaggedJobs")
                        .HasColumnType("boolean");

                    b.Property<string>("AlternateName")
                        .HasColumnType("text");

                    b.Property<List<string>>("Tags")
                        .HasColumnType("text[]");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.HasIndex("Tags");

                    b.ToTable("Judgers");
                });

            modelBuilder.Entity("Karenia.Rurikawa.Models.Test.TestSuite", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int?>("MemoryLimit")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PackageFileId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<List<string>>("Tags")
                        .HasColumnType("text[]");

                    b.Property<Dictionary<string, List<string>>>("TestGroups")
                        .IsRequired()
                        .HasColumnType("jsonb");

                    b.Property<int?>("TimeLimit")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.HasIndex("Name");

                    b.ToTable("TestSuites");
                });
#pragma warning restore 612, 618
        }
    }
}
