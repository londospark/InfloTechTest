using System;
using System.Collections.Generic;
using UserManagement.Data.Entities;
using UserManagement.Shared.DTOs;
using UserManagement.Web.Helpers;

namespace UserManagement.Web.Tests;

public class PagedResultAndMappersTests
{
    [Fact]
    public void PagedResult_HasMore_ComputedCorrectly()
    {
        var items = new List<int> { 1, 2, 3 };
        var page = 1;
        var pageSize = 3;
        var totalCount = 7; // 1*3 < 7 => HasMore true

        var paged = new PagedResultDto<int>(items, page, pageSize, totalCount);

        paged.HasMore.Should().BeTrue();
        paged.Page.Should().Be(page);
        paged.PageSize.Should().Be(pageSize);
        paged.TotalCount.Should().Be(totalCount);
        paged.Items.Should().BeEquivalentTo(items);

        // When total fits exactly, HasMore false
        var paged2 = new PagedResultDto<int>(items, page: 1, pageSize: 3, totalCount: 3);
        paged2.HasMore.Should().BeFalse();
    }

    [Fact]
    public void Mappers_Map_UserLog_To_UserLogDto()
    {
        var now = DateTime.UtcNow;
        var log = new UserLog { Id = 5, UserId = 42, Message = "Test message", CreatedAt = now };

        var dto = Mappers.Map(log);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(5);
        dto.UserId.Should().Be(42);
        dto.Message.Should().Be("Test message");
        dto.CreatedAt.Should().Be(now);
    }
}
