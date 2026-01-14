using System;
using System.Collections.Generic;
using System.Linq;

namespace RMoney
{
    public enum StepKind { Intro, Money, YesNo, Choice, Info, Summary }

    public sealed class Step
    {
        public string Id { get; }
        public string Title { get; }
        public StepKind Kind { get; }
        public string Prompt { get; }

        public string NextId { get; }
        public string YesNextId { get; }
        public string NoNextId { get; }

        public string[] Choices { get; }
        public Action<UserState, int>? RecordChoice { get; }

        public Action<UserState, decimal>? RecordMoney { get; }
        public Action<UserState, bool>? RecordYesNo { get; }
        public Func<UserState, string>? DynamicText { get; }

        private Step(
            string id, string title, StepKind kind, string prompt,
            string nextId = "", string yesNextId = "", string noNextId = "",
            Action<UserState, decimal>? recordMoney = null,
            Action<UserState, bool>? recordYesNo = null,
            Func<UserState, string>? dynamicText = null,
            string[]? choices = null,
            Action<UserState, int>? recordChoice = null)
        {
            Id = id; Title = title; Kind = kind; Prompt = prompt;
            NextId = nextId; YesNextId = yesNextId; NoNextId = noNextId;
            RecordMoney = recordMoney; RecordYesNo = recordYesNo;
            DynamicText = dynamicText; Choices = choices ?? Array.Empty<string>();
            RecordChoice = recordChoice;
        }

        public static Step Intro(string id, string title, string text, string nextId)
            => new Step(id, title, StepKind.Intro, text, nextId: nextId);

        public static Step Money(string id, string title, string prompt, string nextId, Action<UserState, decimal> record)
            => new Step(id, title, StepKind.Money, prompt, nextId: nextId, recordMoney: record);

        public static Step YesNo(string id, string title, string prompt, string yesNextId, string noNextId, Action<UserState, bool> record)
            => new Step(id, title, StepKind.YesNo, prompt, yesNextId: yesNextId, noNextId: noNextId, recordYesNo: record);

        public static Step Choice(string id, string title, string prompt, string[] choices, string nextId, Action<UserState, int> record)
            => new Step(id, title, StepKind.Choice, prompt, nextId: nextId, choices: choices, recordChoice: record);

        public static Step Info(string id, string title, Func<UserState, string> textFunc, string nextId)
            => new Step(id, title, StepKind.Info, "", nextId: nextId, dynamicText: textFunc);

        public static Step Summary(string id, string title)
            => new Step(id, title, StepKind.Summary, "");
    }

    public static class Flow
    {
        public static IReadOnlyDictionary<string, Step> Build()
        {
            var steps = new Dictionary<string, Step>();

            steps["intro"] = Step.Intro(
                "intro",
                $"{Branding.AppName} — {Branding.Tagline}",
                $"{Branding.AppName} follows your workflow logic:\n" +
                "Essentials → Debt → Emergency Fund → Retirement → Goals.\n\n" +
                "Educational only — not financial advice.\n\n" +
                "Click Next to begin.",
                "income");

            steps["income"] = Step.Money("income", "Step 0 — Budget: Income",
                "Enter monthly NET income (after taxes):", "essentials",
                (s, v) => s.MonthlyNetIncome = v);

            steps["essentials"] = Step.Money("essentials", "Step 0 — Budget: Essentials",
                "Enter monthly ESSENTIAL expenses:", "minDebt",
                (s, v) => s.MonthlyEssentials = v);

            steps["minDebt"] = Step.Money("minDebt", "Step 0 — Budget: Minimum Debt",
                "Enter total monthly MINIMUM debt payments (required minimums). If none, enter 0:", "starterFundChoice",
                (s, v) => s.MonthlyMinimumDebtPayments = v);

            steps["starterFundChoice"] = Step.Choice("starterFundChoice", "Step 1 — Starter Emergency Fund Size",
                "Choose your starter emergency fund size:",
                new[] { "$1,000 starter buffer", "1 month of essentials" },
                "dependents",
                (s, idx) => s.PreferStarterFundOneMonth = (idx == 1));

            steps["dependents"] = Step.YesNo("dependents", "Step 1 — Emergency Fund Factors",
                "Do you have dependents relying on your income?",
                "stability", "stability",
                (s, v) => s.HasDependents = v);

            steps["stability"] = Step.YesNo("stability", "Step 1 — Emergency Fund Factors",
                "Is your income unstable (gig/commission/seasonal/variable)?",
                "budgetCheck", "budgetCheck",
                (s, v) => s.IncomeIsUnstable = v);

            steps["budgetCheck"] = Step.Info("budgetCheck", "Budget Check",
                (s) =>
                {
                    decimal surplus = s.MonthlyNetIncome - s.MonthlyEssentials - s.MonthlyMinimumDebtPayments;

                    string breakdown =
                        $"Net income: {s.MonthlyNetIncome:C}\n" +
                        $"Essentials: {s.MonthlyEssentials:C}\n" +
                        $"Minimum debt: {s.MonthlyMinimumDebtPayments:C}\n\n";

                    if (surplus < 0)
                        return breakdown + $"Result: SHORTFALL {surplus:C}\n\n" +
                               "Priority: stabilize cashflow first (cut expenses, raise income, negotiate bills).";

                    return breakdown + $"Result: SURPLUS {surplus:C}\n\n" +
                           "Continue through the workflow to prioritize buffers, debt payoff, and investing.";
                },
                "highInterest");

            steps["highInterest"] = Step.YesNo("highInterest", "Step 2 — High-Interest Debt",
                "Do you have HIGH-INTEREST debt (roughly ≥ 10% APR), like credit cards?",
                "starterInfo", "starterInfo",
                (s, v) => s.HasHighInterestDebt = v);

            steps["starterInfo"] = Step.Info("starterInfo", "Step 3 — Starter Emergency Fund",
                (s) =>
                {
                    var starter = s.PreferStarterFundOneMonth ? "1 month of essentials" : "$1,000";
                    return $"Build a starter emergency fund ({starter}).\n\n" +
                           (s.HasHighInterestDebt
                               ? "High-interest debt exists: keep this starter buffer, prioritize payoff with extra dollars."
                               : "Once this buffer exists, continue.");
                },
                "employerMatch");

            steps["employerMatch"] = Step.YesNo("employerMatch", "Step 4 — Employer Match",
                "Does your employer offer a retirement match?",
                "matchInfo", "debtStrategy",
                (s, v) => s.HasEmployerMatch = v);

            steps["matchInfo"] = Step.Info("matchInfo", "Step 4 — Employer Match",
                (_) => "Contribute enough to capture the FULL match (free money).",
                "debtStrategy");

            steps["debtStrategy"] = Step.Info("debtStrategy", "Step 5 — Debt Strategy",
                (s) =>
                {
                    if (!s.HasHighInterestDebt)
                        return "No high-interest debt reported. Continue to emergency fund + investing steps.";
                    return "High-interest debt reported.\n\n• Pay minimums on all debts\n• Put extra to Avalanche or Snowball";
                },
                "emergencyTarget");

            steps["emergencyTarget"] = Step.Info("emergencyTarget", "Step 6 — Emergency Fund Target",
                (s) =>
                {
                    int months = (s.IncomeIsUnstable || s.HasDependents) ? 6 : 3;
                    return $"Target emergency fund: {months} months of essentials (liquid).";
                },
                "moderateDebt");

            steps["moderateDebt"] = Step.YesNo("moderateDebt", "Step 7 — Moderate-Interest Debt",
                "Do you have MODERATE-INTEREST debt (~4–10% APR)?",
                "moderateInfo", "retirementOrder",
                (s, v) => s.HasModerateInterestDebt = v);

            steps["moderateInfo"] = Step.Info("moderateInfo", "Step 7 — Moderate-Interest Debt",
                (_) => "Balance payoff vs investing; higher APR tends to favor payoff.",
                "retirementOrder");

            steps["retirementOrder"] = Step.Info("retirementOrder", "Step 8 — Retirement Order",
                (_) => "Typical order: match → IRA → increase workplace contributions.",
                "largePurchases");

            steps["largePurchases"] = Step.YesNo("largePurchases", "Step 9 — Near-Term Purchases",
                "Do you have large purchases in the next few years?",
                "largeInfo", "saveRate",
                (s, v) => s.NeedsLargePurchaseSoon = v);

            steps["largeInfo"] = Step.Info("largeInfo", "Step 9 — Near-Term Purchases",
                (_) => "Create a dedicated savings bucket so you don’t raid EF/retirement.",
                "saveRate");

            steps["saveRate"] = Step.YesNo("saveRate", "Step 10 — Savings Rate",
                "Are you saving ~15%+ of gross income for retirement (incl. match)?",
                "workplacePlan", "saveRateInfo",
                (s, v) => s.Saving15PercentOrMore = v);

            steps["saveRateInfo"] = Step.Info("saveRateInfo", "Step 10 — Increase Savings Rate",
                (_) => "Work toward 15% (increase gradually if needed).",
                "workplacePlan");

            steps["workplacePlan"] = Step.YesNo("workplacePlan", "Step 11 — Workplace Plan",
                "Do you have a workplace retirement plan?",
                "selfEmployed", "selfEmployed",
                (s, v) => s.HasWorkplacePlan = v);

            steps["selfEmployed"] = Step.YesNo("selfEmployed", "Step 11 — Self-Employment",
                "Are you self-employed (or have self-employment income)?",
                "selfEmployedInfo", "hsa",
                (s, v) => s.IsSelfEmployed = v);

            steps["selfEmployedInfo"] = Step.Info("selfEmployedInfo", "Self-Employed Options",
                (_) => "Consider SEP IRA / SIMPLE IRA / Solo 401(k) if eligible.",
                "hsa");

            steps["hsa"] = Step.YesNo("hsa", "Step 12 — HSA",
                "Are you eligible for an HSA (qualifying HDHP)?",
                "hsaInfo", "education",
                (s, v) => s.EligibleForHSA = v);

            steps["hsaInfo"] = Step.Info("hsaInfo", "Step 12 — HSA",
                (_) => "If eligible, consider maximizing HSA contributions.",
                "education");

            steps["education"] = Step.YesNo("education", "Step 13 — Education",
                "Do you want to save for kids’ education?",
                "educationInfo", "mortgage",
                (s, v) => s.HasKidsCollegeGoal = v);

            steps["educationInfo"] = Step.Info("educationInfo", "Step 13 — Education",
                (_) => "Consider a 529 plan (state benefits vary).",
                "mortgage");

            steps["mortgage"] = Step.YesNo("mortgage", "Step 14 — Mortgage Prepay",
                "Do you want to evaluate paying extra mortgage principal?",
                "mortgageInfo", "retireEarly",
                (s, v) => s.WantsMortgagePrepayOption = v);

            steps["mortgageInfo"] = Step.Info("mortgageInfo", "Step 14 — Mortgage Prepay",
                (_) => "Mortgage prepay tradeoff: guaranteed return vs liquidity/investing.",
                "retireEarly");

            steps["retireEarly"] = Step.YesNo("retireEarly", "Step 15 — Early Retirement",
                "Do you want to retire early?",
                "retireEarlyInfo", "immediateGoals",
                (s, v) => s.WantsRetireEarly = v);

            steps["retireEarlyInfo"] = Step.Info("retireEarlyInfo", "Step 15 — Early Retirement",
                (_) => "Early retirement usually needs higher savings + taxable investing + liquidity.",
                "immediateGoals");

            steps["immediateGoals"] = Step.YesNo("immediateGoals", "Step 16 — Immediate Goals",
                "Do you have immediate goals to fund soon (travel/move/upgrades)?",
                "immediateGoalsInfo", "summary",
                (s, v) => s.HasImmediateGoals = v);

            steps["immediateGoalsInfo"] = Step.Info("immediateGoalsInfo", "Step 16 — Buckets",
                (_) => "Use buckets: emergency fund, near-term goals, long-term investing.",
                "summary");

            steps["summary"] = Step.Summary("summary", "Your r-money Action Plan");
            return steps;
        }

        public static IReadOnlyList<string> LinearOrder() => new[]
        {
            "intro","income","essentials","minDebt","starterFundChoice","dependents","stability","budgetCheck",
            "highInterest","starterInfo","employerMatch","matchInfo","debtStrategy","emergencyTarget",
            "moderateDebt","moderateInfo","retirementOrder","largePurchases","largeInfo",
            "saveRate","saveRateInfo","workplacePlan","selfEmployed","selfEmployedInfo","hsa","hsaInfo",
            "education","educationInfo","mortgage","mortgageInfo","retireEarly","retireEarlyInfo",
            "immediateGoals","immediateGoalsInfo","summary"
        };

        public static void ComputePlan(UserState s)
        {
            s.Notes.Clear();
            s.Plan.Clear();

            decimal surplus = s.MonthlyNetIncome - s.MonthlyEssentials - s.MonthlyMinimumDebtPayments;
            string starter = s.PreferStarterFundOneMonth ? "1 month of essentials" : "$1,000";
            int efMonths = (s.IncomeIsUnstable || s.HasDependents) ? 6 : 3;

            void Add(int prio, string text, params string[] reasons)
            {
                s.Plan.Add(new PlanItem { Priority = prio, Text = text, Reasons = reasons.ToList() });
            }

            s.Notes.Add($"Budget result: {(surplus < 0 ? "SHORTFALL " : "SURPLUS ")}{surplus:C}");

            if (surplus < 0)
            {
                Add(1, "Stabilize cashflow first: cut expenses, raise income, negotiate bills.",
                    "Triggered by budget shortfall.");
                s.Plan = s.Plan.OrderBy(p => p.Priority).ToList();
                return;
            }

            Add(2, "Cover essentials first.", "Always first.");
            if (s.HasHighInterestDebt) Add(3, "Pay down high-interest debt first.", "High-interest debt = Yes.");
            Add(4, $"Build starter emergency fund ({starter}).", "Starter buffer step.");
            if (s.HasEmployerMatch) Add(5, "Capture full employer match.", "Employer match = Yes.");
            Add(6, $"Build emergency fund to {efMonths} months.", "Dependents/income stability rule.");
            if (s.HasModerateInterestDebt) Add(7, "Balance moderate-interest debt payoff vs investing.", "Moderate-interest debt = Yes.");
            if (s.NeedsLargePurchaseSoon) Add(8, "Save for near-term purchases in a bucket.", "Near-term purchases = Yes.");
            if (!s.Saving15PercentOrMore) Add(9, "Work toward ~15% retirement savings.", "Savings rate < ~15%.");
            if (s.IsSelfEmployed) Add(10, "Consider SEP/SIMPLE/Solo 401(k).", "Self-employed = Yes.");
            if (s.EligibleForHSA) Add(11, "Consider maximizing HSA.", "HSA eligible = Yes.");
            if (s.HasKidsCollegeGoal) Add(12, "Consider a 529 plan.", "Education goal = Yes.");
            if (s.WantsMortgagePrepayOption) Add(13, "Evaluate mortgage prepay tradeoffs.", "Mortgage prepay = Yes.");
            if (s.WantsRetireEarly) Add(14, "Plan for early retirement with taxable investing.", "Retire early = Yes.");
            if (s.HasImmediateGoals) Add(15, "Use buckets for immediate goals.", "Immediate goals = Yes.");

            s.Plan = s.Plan.OrderBy(p => p.Priority).ToList();
        }
    }
}