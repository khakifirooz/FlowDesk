namespace FlowDesk.Web.ViewModels.Projects;

public class ProjectListItemViewModel
{
    public bool CanManage { get; set; }
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}