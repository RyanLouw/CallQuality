using CallQuality.Core.Manager.TrainingManager;
using CallQuality.Core.Manager.TrainingManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace CallQuality.Controllers;

[Route("TrainingRegister")]
public class TrainingRegisterController : Controller
{
    private readonly ITrainingManager _trainingManager;

    public TrainingRegisterController(ITrainingManager trainingManager)
    {
        _trainingManager = trainingManager;
    }

    [HttpGet("")]
    [HttpGet("[action]")]
    public async Task<IActionResult> Index()
    {
        var model = await _trainingManager.GetTrainingRegisterDataAsync();
        return View(model);
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> TrainingDetails([FromQuery] Guid? userId)
    {
        var model = await _trainingManager.GetTrainingDetailsAsync(userId);
        return model is null
            ? NotFound("No required training is available for this agent.")
            : View(model);
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> ExportTraining()
    {
        var bytes = await _trainingManager.ExportTraining();

        var now = DateTime.Now;
        var fileName = $"TrainingRegister_{now:yyyyMMdd_HHmmss}.xlsx";

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName
        );
    }

    [HttpGet("~/Training/GetUsersByGroup/{id}")]
    public async Task<IActionResult> GetUsersByGroup([FromRoute] string id)
    {
        var traineeIds = await _trainingManager.GetTraineesByGroupIdAsync(id);
        return Json(traineeIds);
    }

    [HttpGet("~/Training/GetUsersByLeader/{id}")]
    public async Task<IActionResult> GetUsersByLeader([FromRoute] string id)
    {
        var traineeIds = await _trainingManager.GetTraineesByLeaderIdAsync(id);
        return Json(traineeIds);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(TrainingDetailsPageVM model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var error = await _trainingManager.SaveTrainingRegisterAsync(model, Request.Form.Files);
        if (error is not null)
        {
            return BadRequest(error);
        }

        TempData["Success"] = "Training register added successfully!";
        return RedirectToAction("Index");
    }
}
