using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagement.Data.Entities;

public class UserLog
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// The user this log entry is associated with.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// The log message or description of the action.
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// When the log entry was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
