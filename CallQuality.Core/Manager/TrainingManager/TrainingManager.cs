using CallQuality.Core.DataAccess.CallQualityDataAccess.CQ;
using CallQuality.Core.DataAccess.TrainingRegisterDataAccess;
using CallQuality.Core.Helpers;
using CallQuality.Core.DataAccess.Context.Entities;
using CallQuality.Core.Manager.AssessmentsManager.Models;
using CallQuality.Core.Manager.ExportManager;
using CallQuality.Core.Manager.TrainingManager.Models;
using CallQuality.DataAccess.DataAccess.TrainingRegisterDataAccess;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Data;
using System.Diagnostics;



namespace CallQuality.Core.Manager.TrainingManager;

public class TrainingManager : ITrainingManager
{
    private readonly ITrainingRegisterDataAccess _tc;
    private readonly ICallQualityDataAccess _db;
    private readonly IUserSession _ua;
    private readonly IExportService _ex;



    public TrainingManager(ITrainingRegisterDataAccess tc, ICallQualityDataAccess db, IUserSession ua, IExportService ex)
    {
        _tc = tc;
        _db = db;
        _ua = ua;
        _ex = ex;
    }

    public async Task<TrainingVM> GetTrainingRegisterDataAsync()
    {
        var total = Stopwatch.StartNew();

        var sw = Stopwatch.StartNew();
        var userEntities = await _tc.GetTrainingRegisterUserDataAsync();
        Log.Information(
            "GetTrainingRegisterUserDataAsync took {ElapsedMs}ms. Retrieved {Count} users",
            sw.ElapsedMilliseconds,
            userEntities.Count());

        sw.Restart();
        var users = userEntities
            .Select(x => new TrainingUserTrainingVM(x))
            .OrderByDescending(x => x.TrainingDate)
            .ToList();

        Log.Information(
            "Mapping UserTrainings took {ElapsedMs}ms. Mapped {Count} users",
            sw.ElapsedMilliseconds,
            users.Count);

        sw.Restart();
        var existingRegisterEntities = await _tc.GetAllTrainingRegistersAsync();
        Log.Information(
            "GetAllTrainingRegistersAsync took {ElapsedMs}ms. Retrieved {Count} registers",
            sw.ElapsedMilliseconds,
            existingRegisterEntities.Count());

        sw.Restart();
        var existingRegisters = existingRegisterEntities
            .Select(x => new ExistingRegisterItemVM(x))
            .OrderByDescending(x => x.CreatedOn)
            .ToList();

        Log.Information(
            "Mapping ExistingRegisters took {ElapsedMs}ms. Mapped {Count} registers",
            sw.ElapsedMilliseconds,
            existingRegisters.Count);

        sw.Restart();
        var operatorEntities = await _db.GetOperatorAssignmentReportAsync();
        Log.Information(
            "GetOperatorAssignmentReportAsync took {ElapsedMs}ms. Retrieved {Count} operators",
            sw.ElapsedMilliseconds,
            operatorEntities.Count());

        sw.Restart();
        var operators = operatorEntities
            .Select(x => new OperatorAssignmentReportVM(x))
            .ToList();

        Log.Information(
            "Mapping OperatorAssignments took {ElapsedMs}ms. Mapped {Count} operators",
            sw.ElapsedMilliseconds,
            operators.Count);

        var requiredTrainings = await GetRequiredTrainingsAsync();

        Log.Information(
            "GetTrainingRegisterDataAsync completed in {ElapsedMs}ms",
            total.ElapsedMilliseconds);




        return new TrainingVM
        {
            UserTrainings = users,
            ExistingRegisters = existingRegisters,
            OperatorAssignments = operators,
            RequiredTrainings = requiredTrainings
        };
    }

    public async Task<TrainingDetailsPageVM?> GetTrainingDetailsAsync(Guid? userId)
    {
        var total = Stopwatch.StartNew();

        var sw = Stopwatch.StartNew();

        var managers = await GetManagersAsync();
        Log.Information(
            "GetManagersAsync took {ElapsedMs}ms. Retrieved {Count} managers",
            sw.ElapsedMilliseconds,
            managers?.Count() ?? 0);

        sw.Restart();
        var facilitators = await GetFacilitatorDropdown();
        Log.Information(
            "GetFacilitatorDropdown for facilitators took {ElapsedMs}ms. Retrieved {Count} facilitators",
            sw.ElapsedMilliseconds,
            facilitators?.Count() ?? 0);

        sw.Restart();
        var trainees = await GetFacilitatorDropdown();
        Log.Information(
            "GetFacilitatorDropdown for trainees took {ElapsedMs}ms. Retrieved {Count} trainees",
            sw.ElapsedMilliseconds,
            trainees?.Count() ?? 0);

        sw.Restart();
        var durations = GetTimeDurationOptions();
        Log.Information(
            "GetTimeDurationOptions took {ElapsedMs}ms. Retrieved {Count} duration options",
            sw.ElapsedMilliseconds,
            durations?.Count() ?? 0);

        sw.Restart();
        var groups = await GetGroupsDropdown();
        Log.Information(
            "GetGroupsDropdown took {ElapsedMs}ms. Retrieved {Count} groups",
            sw.ElapsedMilliseconds,
            groups?.Count() ?? 0);

        sw.Restart();
        var newTrainingRegisterView = new NewTrainingRegisterVM
        {
            DueDate = DateTime.Today
        };

        var loggedInUserId = _ua.GetCurrentUserId();
        var loggedInUserIdString = loggedInUserId?.ToString();

        var loggedInFacilitator = facilitators.FirstOrDefault(f =>
            string.Equals(
                f.UserId?.Trim(),
                loggedInUserIdString?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (loggedInFacilitator != null)
        {
            newTrainingRegisterView.TrainingFacilitator = loggedInFacilitator;

        }
        else
        {
            Log.Warning(
                "Logged-in user {UserId} was not found in the facilitator dropdown",
                loggedInUserId);
        }

        Log.Information(
            "Initial NewTrainingRegisterVM setup took {ElapsedMs}ms",
            sw.ElapsedMilliseconds);

        if (userId is Guid traineeId)
        {
            var requiredTraining = (await GetRequiredTrainingsAsync(traineeId)).SingleOrDefault();
            if (requiredTraining is null)
            {
                return null;
            }

            newTrainingRegisterView.RequiredTrainingUserId = traineeId;
            newTrainingRegisterView.RequiredAssessmentIds = requiredTraining.AssessmentIds;
            newTrainingRegisterView.SelectedTraineeIds = [traineeId.ToString()];
            newTrainingRegisterView.TrainingTopic = "Call Quality Counselling";
            newTrainingRegisterView.TimeDuration = durations.FirstOrDefault()?.DurationValue ?? 0;
            newTrainingRegisterView.TrainingDescription = string.Join(Environment.NewLine,
                requiredTraining.MissedQuestions.Select(question =>
                    $"• {question.QuestionValue} (Missed {question.MissedCount} time{(question.MissedCount == 1 ? "" : "s")})"));
        }

        Log.Information(
            "GetTrainingDetailsAsync completed in {ElapsedMs}ms. UserId: {UserId}",
            total.ElapsedMilliseconds,
            userId);

        return new TrainingDetailsPageVM
        {
            Leaders = managers,
            Facilitators = facilitators,
            Trainees = trainees,
            Groups = groups,
            TimeDurations = durations,
            NewTraining = newTrainingRegisterView
        };
    }
   
    
    public async Task<byte[]> ExportTraining()
    {
        var modle = await GetTrainingRegisterDataAsync();
        return await _ex.DownloadTrainingReportExcelAsync(modle);

    }

    public async Task<List<UserAddVM>> GetManagersAsync()
    {
        var managerEntities = await _db.GetManagersAsync();

        var managers = managerEntities
            .Select(x => new UserAddVM(x))
            .OrderBy(x => x.DisplayName)
            .ToList();

        managers.Insert(0, new UserAddVM
        {
            UserId = "",
            DisplayName = "-- Please select a manager --"
        });

        return managers;
    }


    public async Task<List<AssessmentsManager.Models.UserAddVM>> GetFacilitatorDropdown()
    {
        var allUsers = (await _db.GetAllUsersTraining()).Select(x => new AssessmentsManager.Models.UserAddVM(x));

        var facilitators = allUsers
            .OrderBy(u => u.DisplayName)
            .ToList();

        return facilitators;
    }



    public List<DurationOption> GetTimeDurationOptions()
    {
        var durations = new List<DurationOption>();

        for (int minutes = 30; minutes <= 240; minutes += 30)
        {
            string text;

            if (minutes % 60 == 0)
            {
                text = $"{minutes / 60}-Hr";
            }
            else
            {
                text = $"{minutes / 60}-Hr : {minutes % 60}-Min";
            }

            durations.Add(new DurationOption
            {
                DurationValue = minutes,
                DurationText = text
            });
        }

        return durations;
    }


    public async Task<List<UserAddVM.Group>> GetGroupsDropdown()
    {
        var groupEntities = await _tc.GetGroupAsync();

        return groupEntities
            .Select(x => new UserAddVM.Group(x))
            .OrderBy(g => g.DisplayName)
            .ToList();
    }




    public async Task<List<string>> GetTraineesByLeaderIdAsync(string leaderId)
    {
        if (string.IsNullOrWhiteSpace(leaderId))
            return new List<string>();

        var users = await _db.GetAllUsersAsync();

        return users
            .Where(u => !string.IsNullOrWhiteSpace(u.Manager_ID))
            .Where(u => string.Equals(
                u.Manager_ID.Trim(),
                leaderId.Trim(),
                StringComparison.OrdinalIgnoreCase))
            .Select(u => u.ID_Guid.ToString())
            .Where(adUserId => !string.IsNullOrWhiteSpace(adUserId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<List<string>> GetTraineesByGroupIdAsync(string Groupid)
    {
        var users = await _tc.GetGroupUsersAsync(Groupid);
        return users;
    }


    private async Task<List<RequiredTrainingGroupVM>> GetRequiredTrainingsAsync(Guid? traineeId = null)
    {
        var currentUserId = _ua.GetCurrentUserId();
        if (currentUserId is null)
        {
            return [];
        }

        var roles = await _ua.GetUserRolesAsync();
        var canViewAll = roles.Contains("Manager") || roles.Contains("Reassessor");
        if (!canViewAll && !roles.Contains("Assessor"))
        {
            return [];
        }

        var users = (await _db.GetAllUsersAsync())
            .Select(user => new
            {
                User = user,
                Id = user.ID_Guid ?? (Guid.TryParse(user.ID, out var id) ? id : Guid.Empty)
            })
            .Where(item => item.Id != Guid.Empty)
            .DistinctBy(item => item.Id)
            .ToDictionary(item => item.Id, item => item.User);

        var now = DateTime.Now;
        var today = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        string[]? operatorIds = traineeId is Guid selectedId ? [selectedId.ToString()] : null;
        if (!canViewAll)
        {
            var assessorName = _ua.GetUserName().Trim();
            var assessorEmail = _ua.GetCurrentUserEmail()?.Trim();
            var canMatchByName = users.TryGetValue(currentUserId.Value, out var assessorUser) &&
                string.Equals(assessorUser.DisplayName?.Trim(), assessorName, StringComparison.OrdinalIgnoreCase) &&
                users.Values.Count(user => string.Equals(user.DisplayName?.Trim(), assessorName,
                    StringComparison.OrdinalIgnoreCase)) == 1;
            var assessorIds = (await _db.GetAllAssessorsAsync())
                .Where(assessor =>
                    string.Equals(assessor.AssessorName?.Trim(), currentUserId.Value.ToString(),
                        StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(assessorEmail) &&
                        string.Equals(assessor.AssessorName?.Trim(), assessorEmail, StringComparison.OrdinalIgnoreCase)) ||
                    (canMatchByName &&
                        string.Equals(assessor.AssessorName?.Trim(), assessorName, StringComparison.OrdinalIgnoreCase)))
                .Select(assessor => assessor.AssessorId)
                .Take(2)
                .ToList();

            if (assessorIds.Count != 1)
            {
                return [];
            }

            var assessorId = assessorIds[0];
            var assignments = await _db.GetOperatorAssignmentsAsync();
            operatorIds = assignments
                .Where(assignment => assignment.AssessorId == assessorId ||
                    (assignment.AssessorIdSecondary == assessorId &&
                     assignment.SecondaryStartDate < today.AddDays(1) &&
                     assignment.SecondaryEndDate >= today))
                .Select(assignment => Guid.TryParse(assignment.OperatorId, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty && (traineeId is null || id == traineeId))
                .Distinct()
                .Select(id => id.ToString())
                .ToArray();

            if (operatorIds.Length == 0)
            {
                return [];
            }
        }

        var assessments = await _db.GetRequiredTrainingAssessmentsAsync(
            monthStart, monthStart.AddMonths(1), operatorIds);
        if (assessments.Count == 0)
        {
            return [];
        }

        return assessments
            .GroupBy(assessment => Guid.TryParse(assessment.OperatorId, out var id) ? id : Guid.Empty)
            .Where(group => users.ContainsKey(group.Key))
            .Select(group =>
            {
                var user = users[group.Key];
                return new RequiredTrainingGroupVM
                {
                    ID_GUID = group.Key,
                    AgentName = user.DisplayName ?? string.Empty,
                    Department = user.Department ?? string.Empty,
                    Extension = user.Extension ?? string.Empty,
                    AverageScore = group.Average(assessment => assessment.Percentage),
                    AssessmentIds = group.Select(assessment => assessment.AssessmentId).Order().ToList(),
                    MissedQuestions = group.SelectMany(assessment => assessment.MissedQuestions)
                        .GroupBy(question => question, StringComparer.OrdinalIgnoreCase)
                        .Select(questions => new MissedQuestion
                        {
                            QuestionValue = questions.Key,
                            MissedCount = questions.Count()
                        })
                        .OrderBy(question => question.QuestionValue)
                        .ToList()
                };
            }).OrderBy(group => group.AgentName).ToList();
    }

    public async Task<string?> SaveTrainingRegisterAsync(TrainingDetailsPageVM model, IFormFileCollection? files)
    {
        var trainees = model.NewTraining.SelectedTraineeIds;
        var requiredUserId = model.NewTraining.RequiredTrainingUserId;
        var submittedAssessmentIds = model.NewTraining.RequiredAssessmentIds;
        var users = await GetFacilitatorDropdown();
        var selectedUsers = users.Where(user => trainees.Contains(user.UserId, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (selectedUsers.Count == 0)
        {
            return "Select at least one valid trainee.";
        }

        var dto = MapToDTO(model, users);
        var username = _ua.GetUserName();
        var email = _ua.GetCurrentUserEmail();
        if (requiredUserId is null)
        {
            if (submittedAssessmentIds.Count != 0)
            {
                return "The required training selection is invalid.";
            }

            var attachmentUrls = files is null ? [] : await UploadFilesAsync(files);
            var manualResult = await _tc.SaveNewTrainingAsync(dto, selectedUsers, attachmentUrls, username, email);
            return manualResult.Success ? null : "The Training Register could not be saved.";
        }

        if (_ua.GetCurrentUserId() is not Guid trainerId || selectedUsers.Count != 1 ||
            !Guid.TryParse(selectedUsers[0].UserId, out var selectedId) || selectedId != requiredUserId ||
            trainees.Count != 1)
        {
            return "Save required training for the selected agent only.";
        }

        var required = (await GetRequiredTrainingsAsync(selectedId)).SingleOrDefault();
        if (required is null || !required.AssessmentIds.SequenceEqual(submittedAssessmentIds.Order()))
        {
            return "The required assessments or your access have changed. Reopen the agent’s training from Required Training.";
        }

        var uploadedFileUrls = files is null ? [] : await UploadFilesAsync(files);
        var submissionId = Guid.NewGuid();
        var trainedAt = DateTime.UtcNow;
        var links = required.AssessmentIds.Select(id => new AssessmentTrainingLink
        {
            AssessmentId = id,
            SubmissionId = submissionId,
            TrainedByUserId = trainerId,
            TrainedAtUtc = trainedAt
        }).ToList();

        if (!await _db.TryReserveAssessmentTrainingAsync(links))
        {
            return "Training has already been submitted or is awaiting confirmation for these assessments.";
        }

        long? trainingId = null;
        try
        {
            var result = await _tc.SaveNewTrainingAsync(dto, selectedUsers, uploadedFileUrls, username, email);
            trainingId = result.TrainingId;
            if (result.Success && trainingId is long id &&
                await _db.CompleteAssessmentTrainingAsync(submissionId, id) == links.Count)
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Training confirmation failed for submission {SubmissionId}, register {TrainingId}",
                submissionId, trainingId);
        }

        Log.Warning("Training submission {SubmissionId} needs reconciliation. Register: {TrainingId}",
            submissionId, trainingId);
        return $"The register could not be confirmed. Ask support to check submission {submissionId} before retrying.";
    }

    private NewTrainingRegister MapToDTO(
    TrainingDetailsPageVM model,
    List<UserAddVM> users)
    {
        var facilitatorId = model.NewTraining.TrainingFacilitator.UserId;
        var facilitator = users.FirstOrDefault(u => u.UserId == facilitatorId);
        if (facilitator == null)
            throw new Exception("Facilitator not found in list.");

        return new NewTrainingRegister
        {
            Name = model.NewTraining.TrainingTopic,
            Description = model.NewTraining.TrainingDescription,
            TrainingDate = DateTime.Now,
            TrainingDueDate = model.NewTraining.DueDate,
            AddNewAssessment = false,
            selectedTimeDuration = model.NewTraining.TimeDuration.ToString(),
            trainingFacilitator = facilitator,
            trainingFacilitatorMail = facilitator.EmailAddress,
            FacilitatorSigned = "Signed",
            SystemID = string.IsNullOrEmpty(model.NewTraining.SystemId) ? null : Guid.Parse(model.NewTraining.SystemId),
            IsCallQuality = true
        };
    }



    private async Task<List<string>> UploadFilesAsync(IFormFileCollection files)
    {
        var uploadedFileUrls = new List<string>();

        if (files == null || files.Count == 0)
            return uploadedFileUrls;


        var user = _ua.GetUserName();
        var authToken = _ua.GetCurrentUserAuthToken();

        if (string.IsNullOrEmpty(authToken))
        {
            Log.Error("OID not found — cannot upload files.");
            return uploadedFileUrls;
        }

        foreach (var file in files)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            byte[] fileBytes = ms.ToArray();

            string fileUrl = await _tc.UploadTrainingFile(fileBytes, file.FileName, authToken);

            if (!string.IsNullOrEmpty(fileUrl))
            {
                uploadedFileUrls.Add(fileUrl);
            }
        }

        return uploadedFileUrls;
    }


}