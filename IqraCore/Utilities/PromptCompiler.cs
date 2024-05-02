using IqraCore.Entities.Business;
using IqraCore.Interfaces.AI;

namespace IqraCore.Utilities
{
    public class PromptCompiler
    {
        public static string CompilePromptFromBusiness(Business business, string businessDefaultLanguage, Dictionary<string, string> SystemPromptVariables, string compiledInitialMessage)
        {
            string DnyamicStringPrompt = File.ReadAllText("SystemPromptDynamic.txt");

            // General Information
            DnyamicStringPrompt = DnyamicStringPrompt.Replace(
                "<GeneralInformation></GeneralInformation>",
                $"<GeneralInformation>\n{business.BusinessPromptData.BusinessGeneralData[businessDefaultLanguage]}\n</GeneralInformation>"
            );

            // About And Features Information
            DnyamicStringPrompt = DnyamicStringPrompt.Replace(
                "<AboutAndFeaturesInformation></AboutAndFeaturesInformation>",
                $"<AboutAndFeaturesInformation>\n{business.BusinessPromptData.BusinessAboutAndFeatures[businessDefaultLanguage]}\n</AboutAndFeaturesInformation>"
            );

            // Working Hours Information
            DnyamicStringPrompt = DnyamicStringPrompt = DnyamicStringPrompt.Replace(
                "<WorkingHoursInformation></WorkingHoursInformation>",
                $"<WorkingHoursInformation>\n{ConvertBusinessWorkingHoursToString(business.BusinessPromptData.BusinessWorkingHours)}\n</WorkingHoursInformation>"
            );

            // Team/Employee Information
            DnyamicStringPrompt = DnyamicStringPrompt.Replace(
                "<TeamEmployeeInformation></TeamEmployeeInformation>",
                $"<TeamEmployeeInformation>\n{business.BusinessPromptData.BusinessTeamMembers[businessDefaultLanguage]}\n</TeamEmployeeInformation>"
            );

            // Services Information
            DnyamicStringPrompt = DnyamicStringPrompt.Replace(
                "<ServicesInformation></ServicesInformation>",
                $"<ServicesInformation>\n{business.BusinessPromptData.BusinessServices[businessDefaultLanguage]}\n</ServicesInformation>"
            );

            // Apply Template Variables
            DnyamicStringPrompt = ApplyTemplateVariablesToString(DnyamicStringPrompt, SystemPromptVariables);

            return DnyamicStringPrompt;
        }

        public static string ConvertBusinessWorkingHoursToString(BusinessWorkingHours businessWorkingHours)
        {
            string result = $"";

            // Sunday
            if (businessWorkingHours.Sunday.IsWeekend)
            {
                result += $"Sunday closed.";
            }
            else
            {
                result += $"Sunday: {string.Join('-', businessWorkingHours.Sunday.WorkingHours)}";
            }

            result += $"\n";

            // Monday
            if (businessWorkingHours.Monday.IsWeekend)
            {
                result += $"Monday closed.";
            }
            else
            {
                result += $"Monday: {string.Join('-', businessWorkingHours.Monday.WorkingHours)}";
            }

            result += $"\n";

            // Tuesday
            if (businessWorkingHours.Tuesday.IsWeekend)
            {
                result += $"Tuesday closed.";
            }
            else
            {
                result += $"Tuesday: {string.Join('-', businessWorkingHours.Tuesday.WorkingHours)}";
            }

            result += $"\n";

            // Wednesday
            if (businessWorkingHours.Wednesday.IsWeekend)
            {
                result += $"Wednesday closed.";
            }
            else
            {
                result += $"Wednesday: {string.Join('-', businessWorkingHours.Wednesday.WorkingHours)}";
            }

            result += $"\n";

            // Thursday
            if (businessWorkingHours.Thursday.IsWeekend)
            {
                result += $"Thursday closed.";
            }
            else
            {
                result += $"Thursday: {string.Join('-', businessWorkingHours.Thursday.WorkingHours)}";
            }

            result += $"\n";

            // Friday
            if (businessWorkingHours.Friday.IsWeekend)
            {
                result += $"Friday closed.";
            }
            else
            {
                result += $"Friday: {string.Join('-', businessWorkingHours.Friday.WorkingHours)}";
            }

            result += $"\n";

            // Saturday
            if (businessWorkingHours.Saturday.IsWeekend)
            {
                result += $"Saturday closed.";
            }
            else
            {
                result += $"Saturday: {string.Join('-', businessWorkingHours.Saturday.WorkingHours)}";
            }

            return result;
        }

        public static Dictionary<string, string> GetDynamicTimeVariables()
        {
            Dictionary<string, string> dynamicTimeVariables = new Dictionary<string, string>();

            dynamicTimeVariables["DATETIME_TODAY"] = DateTime.Now.ToString();
            dynamicTimeVariables["DATE_TODAY"] = DateTime.Now.ToString("dd-MM-yyyy");
            dynamicTimeVariables["FULL_MONTH_TODAY"] = DateTime.Now.ToString("MMMM");
            dynamicTimeVariables["DATE_AND_FULL_DAY_TODAY"] = DateTime.Now.ToString("dddd, d");
            dynamicTimeVariables["YEAR_TODAY"] = DateTime.Now.ToString("yyyy");
            dynamicTimeVariables["TIME_RIGHT_NOW"] = DateTime.Now.ToString("HH:mm");

            return dynamicTimeVariables;
        }

        public static string ApplyTemplateVariablesToString(string template, Dictionary<string, string> variables)
        {
            string result = template;

            foreach (var variable in variables)
            {
                result = result.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }

            return result;
        }
    }
}
