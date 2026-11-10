using System.ComponentModel.DataAnnotations;
using FlowDesk.Application.DTOs.ProjectMembers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FlowDesk.Web.ViewModels.ProjectMembers;

public class ProjectMembersViewModel
{
    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    [Display(Name = "عضو")]
    [Range(1, int.MaxValue, ErrorMessage = "یک عضو انتخاب کن.")]
    public int TeamMemberId { get; set; }

    [BindNever]
    [ValidateNever]
    public string ProjectName { get; set; } = string.Empty;

    [BindNever]
    [ValidateNever]
    public List<SelectListItem> AvailableMembers { get; set; } = new();

    [BindNever]
    [ValidateNever]
    public List<ProjectMemberListItemDto> Members { get; set; } = new();
}