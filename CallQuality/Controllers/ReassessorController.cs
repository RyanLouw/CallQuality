using CallQuality.Core.Manager.ExportManager.Models;
using CallQuality.Core.Manager.ReportManager;
using CallQuality.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallQuality.Controllers;

[Authorize(Policy = "Reassessor")]
[Route("Reassessor")]
public class ReassessorController(IReportManager reports) : Controller
{
    [HttpGet("")]
    [HttpGet(nameof(Index))]
    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;
        var model = await reports.GetReassessorHomeOverviewAsync(now.AddMonths(-1), now);

        HttpContext.Session.SetString("ActiveViewMode", "Reassessor");
        return View(model);
    }

    [HttpGet(nameof(Export))]
    public async Task<IActionResult> Export()
    {
        var now = DateTime.Now;
        var model = await reports.GetReassessorHomeOverviewAsync(now.AddMonths(-1), now);

        var rows = model.Departments.SelectMany(department => department.Agents.Select(agent => new
        {
            Department = department.DepartmentName,
            AgentName = agent.DisplayName,
            TimesReassessed = agent.AssessmentsDone
        }));

        var bytes = ExcelExportHelper.ExportDataTablesToExcel(
        [
            new ExcelExportDTO
            {
                AssessorsName = "Reassessment Overview",
                AssessorChartData = ExcelExportHelper.ToDataTable(rows)
            }
        ]);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ReassessmentOverview_{now:yyyyMMdd_HHmm}.xlsx");
    }
}
