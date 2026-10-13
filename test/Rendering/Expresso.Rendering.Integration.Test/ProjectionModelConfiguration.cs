using Expresso.Rendering.TestCases;
#if NET8_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
#else
#if NETFRAMEWORK
using System.Data.Entity;
#endif
#endif

namespace Expresso.Rendering.Integration.Test;

internal static class ProjectionModelConfiguration
{
#if NET8_0_OR_GREATER || NETFRAMEWORK
    internal static void Configure(
#if NET8_0_OR_GREATER
        ModelBuilder
#else
        DbModelBuilder
#endif
        model)
    {
        var parent = model.Entity<ProjectionParent>().ToTable("widget_projection_parent");
        parent.HasKey(x => new { x.Tenant, x.Id });
        parent.Property(x => x.Tenant).HasColumnName("tenant");
        parent.Property(x => x.Id).HasColumnName("id");
        parent.Property(x => x.Title).HasColumnName("title");
        parent.Property(x => x.Notes).HasColumnName("notes");
        var child = model.Entity<ProjectionChild>().ToTable("widget_projection_child");
        child.HasKey(x => new { x.Tenant, x.Id });
        child.Property(x => x.Tenant).HasColumnName("tenant");
        child.Property(x => x.Id).HasColumnName("id");
        child.Property(x => x.ParentId).HasColumnName("parent_id");
        child.Property(x => x.Name).HasColumnName("name");
        child.Property(x => x.Payload).HasColumnName("payload");
        var leaf = model.Entity<ProjectionLeaf>().ToTable("widget_projection_leaf");
        leaf.HasKey(x => new { x.Tenant, x.Id });
        leaf.Property(x => x.Tenant).HasColumnName("tenant");
        leaf.Property(x => x.Id).HasColumnName("id");
        leaf.Property(x => x.ChildId).HasColumnName("child_id");
        leaf.Property(x => x.Value).HasColumnName("value");
#if NET8_0_OR_GREATER
        parent.HasMany(x => x.Children).WithOne().HasForeignKey(x => new { x.Tenant, x.ParentId });
        child.HasMany(x => x.Leaves).WithOne().HasForeignKey(x => new { x.Tenant, x.ChildId });
        parent.Property(x => x.Tenant).ValueGeneratedNever();
        parent.Property(x => x.Id).ValueGeneratedNever();
        child.Property(x => x.Tenant).ValueGeneratedNever();
        child.Property(x => x.Id).ValueGeneratedNever();
        leaf.Property(x => x.Tenant).ValueGeneratedNever();
        leaf.Property(x => x.Id).ValueGeneratedNever();
#else
        parent.HasMany(x => x.Children).WithRequired().HasForeignKey(x => new { x.Tenant, x.ParentId });
        child.HasMany(x => x.Leaves).WithRequired().HasForeignKey(x => new { x.Tenant, x.ChildId });
#endif
    }
#endif
}
