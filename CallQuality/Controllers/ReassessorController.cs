using CallQuality.Core.Manager.AssessmentsManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallQuality.Controllers;

[Authorize(Policy = "Reassessor")]
[Route("Reassessor")]
public class ReassessorController(ICallAssessmentManager assessments) : Controller
{
    [HttpGet("")]
    [HttpGet(nameof(Index))]
    public async Task<IActionResult> Index()
    {
        var model = await assessments.GetPagedAssessmentsAsync(
            DateTime.Now.AddMonths(-1),
            possiblePage: 1,
            reassessedPage: 1,
            pageSize: 1,
            search: null,
            activeTab: "possible");

        HttpContext.Session.SetString("ActiveViewMode", "Reassessor");
        return View(model);
    }
}
