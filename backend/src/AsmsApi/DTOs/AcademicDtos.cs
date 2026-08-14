using System.ComponentModel.DataAnnotations;

namespace AsmsApi.DTOs;

public record ClassRoomDto(int Id, string Name, int StudentCount, int SubjectCount);

public record CreateClassRoomRequest([Required, MaxLength(150)] string Name);

public record SubjectDto(int Id, string Name, int ClassRoomId, string ClassRoomName);

public record CreateSubjectRequest(
    [Required, MaxLength(150)] string Name,
    [Required] int ClassRoomId
);

public record AssignTeacherRequest(
    [Required] int TeacherId,
    [Required] int SubjectId
);
