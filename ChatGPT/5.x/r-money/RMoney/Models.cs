using System.Collections.Generic;

namespace RMoney
{
    public sealed class UserState
    {
        public decimal MonthlyNetIncome { get; set; }
        public decimal MonthlyEssentials { get; set; }
        public decimal MonthlyMinimumDebtPayments { get; set; }

        public bool HasHighInterestDebt { get; set; }
        public bool HasModerateInterestDebt { get; set; }
        public bool HasEmployerMatch { get; set; }
        public bool NeedsLargePurchaseSoon { get; set; }
        public bool Saving15PercentOrMore { get; set; }
        public bool HasWorkplacePlan { get; set; }
        public bool IsSelfEmployed { get; set; }
        public bool EligibleForHSA { get; set; }
        public bool HasKidsCollegeGoal { get; set; }
        public bool WantsMortgagePrepayOption { get; set; }
        public bool WantsRetireEarly { get; set; }
        public bool HasImmediateGoals { get; set; }

        public bool IncomeIsUnstable { get; set; }
        public bool HasDependents { get; set; }
        public bool PreferStarterFundOneMonth { get; set; } // false => $1,000

        public List<string> Notes { get; set; } = new();
        public List<PlanItem> Plan { get; set; } = new();
    }

    public sealed class PlanItem
    {
        public int Priority { get; set; }
        public string Text { get; set; } = "";
        public List<string> Reasons { get; set; } = new();
        public bool Done { get; set; } = false;
    }
}