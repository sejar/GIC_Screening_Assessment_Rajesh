using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BankingSystem
{
    public class Transaction
    {
        public string Id { get; }
        public DateTime Date { get; }
        public string Account { get; }
        public char Type { get; }
        public decimal Amount { get; }

        public Transaction(string id, DateTime date, string account, char type, decimal amount)
        {
            Id = id;
            Date = date;
            Account = account;
            Type = type;
            Amount = amount;
        }
    }

    public class InterestRule
    {
        public DateTime Date { get; }
        public string RuleId { get; }
        public decimal Rate { get; }

        public InterestRule(DateTime date, string ruleId, decimal rate)
        {
            Date = date;
            RuleId = ruleId;
            Rate = rate;
        }
    }

    public class Account
    {
        public string AccountId { get; }
        public List<Transaction> Transactions { get; }
        public decimal Balance => Transactions.Sum(t => t.Type == 'D' ? t.Amount : -t.Amount);

        public Account(string accountId)
        {
            AccountId = accountId;
            Transactions = new List<Transaction>();
        }

        public void AddTransaction(Transaction transaction)
        {
            Transactions.Add(transaction);
        }
    }

    public class BankingSystem
    {
        private readonly Dictionary<string, Account> accounts = new();
        private readonly SortedDictionary<DateTime, InterestRule> interestRules = new();

        public void Run()
        {
            string input;
            do
            {
                Console.WriteLine("Welcome to AwesomeGIC Bank! What would you like to do?");
                Console.WriteLine("[T] Input transactions");
                Console.WriteLine("[I] Define interest rules");
                Console.WriteLine("[P] Print statement");
                Console.WriteLine("[Q] Quit");
                Console.Write(">");
                input = Console.ReadLine();

                switch (input?.ToUpper())
                {
                    case "T":
                        InputTransactions();
                        break;
                    case "I":
                        DefineInterestRules();
                        break;
                    case "P":
                        PrintStatementMenu();
                        break;
                }
            } while (input?.ToUpper() != "Q");

            Console.WriteLine("Thank you for banking with AwesomeGIC Bank.\nHave a nice day!");
        }

        private void InputTransactions()
        {
            string input;
            do
            {
                Console.WriteLine("Please enter transaction details in <Date> <Account> <Type> <Amount> format " +
                                  "(or enter blank to go back to main menu):");
                Console.Write(">");
                input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) break;

                var parts = input.Split(' ');
                if (parts.Length != 4 ||
                    !DateTime.TryParseExact(parts[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                    string.IsNullOrEmpty(parts[1]) ||
                    !char.TryParse(parts[2].ToUpper(), out var type) ||
                    !decimal.TryParse(parts[3], out var amount) || amount <= 0 || (type == 'W' && !accounts.ContainsKey(parts[1])) || 
                    (type == 'W' && accounts[parts[1]].Balance < amount))
                {
                    Console.WriteLine("Invalid input. Please try again.");
                    continue;
                }

                if (!accounts.ContainsKey(parts[1]))
                {
                    accounts[parts[1]] = new Account(parts[1]);
                }

                var transactionId = $"{date:yyyyMMdd}-{accounts[parts[1]].Transactions.Count + 1:00}";
                var transaction = new Transaction(transactionId, date, parts[1], type, amount);
                accounts[parts[1]].AddTransaction(transaction);

                PrintAccountStatement(parts[1]);
            } while (true);
        }

        private void DefineInterestRules()
        {
            string input;
            do
            {
                Console.WriteLine("Please enter interest rules details in <Date> <RuleId> <Rate in %> format " +
                                  "(or enter blank to go back to main menu):");
                Console.Write(">");
                input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) break;

                var parts = input.Split(' ');
                if (parts.Length != 3 ||
                    !DateTime.TryParseExact(parts[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                    string.IsNullOrEmpty(parts[1]) ||
                    !decimal.TryParse(parts[2], out var rate) || rate <= 0 || rate >= 100)
                {
                    Console.WriteLine("Invalid input. Please try again.");
                    continue;
                }

                interestRules[date] = new InterestRule(date, parts[1], rate);
                PrintInterestRules();
            } while (true);
        }

        private void PrintInterestRules()
        {
            Console.WriteLine("Interest rules:");
            Console.WriteLine("| Date     | RuleId | Rate (%) |");
            foreach (var rule in interestRules)
            {
                Console.WriteLine($"| {rule.Key:yyyyMMdd} | {rule.Value.RuleId} | {rule.Value.Rate,8:F2} |");
            }
        }

        private void PrintStatementMenu()
        {
            Console.WriteLine("Please enter account and month to generate the statement <Account> <Year><Month> " +
                              "(or enter blank to go back to main menu):");
            Console.Write(">");
            string input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) return;

            var parts = input.Split(' ');
            if (parts.Length != 2 || !accounts.ContainsKey(parts[0]))
            {
                Console.WriteLine("Invalid input. Please try again.");
                return;
            }

            var account = accounts[parts[0]];
            int year = int.Parse(parts[1].Substring(0, 4));
            int month = int.Parse(parts[1].Substring(4, 2));
            var statementDate = new DateTime(year, month, 1);

            Console.WriteLine($"Account: {account.AccountId}");
            Console.WriteLine("| Date     | Txn Id      | Type | Amount | Balance |");

            decimal balance = 0;
            foreach (var transaction in account.Transactions.Where(t => t.Date.Month == month && t.Date.Year == year))
            {
                balance += transaction.Type == 'D' ? transaction.Amount : -transaction.Amount;
                Console.WriteLine($"| {transaction.Date:yyyyMMdd} | {transaction.Id} | {transaction.Type} | {transaction.Amount,6:F2} | {balance,8:F2} |");
            }

            // Calculate interest
            if (interestRules.Any())
            {
                var daysInMonth = DateTime.DaysInMonth(year, month);
                DateTime startOfMonth = statementDate;
                DateTime endOfMonth = statementDate.AddMonths(1).AddDays(-1);
                decimal totalInterest = 0;

                for (var day = 1; day <= daysInMonth; day++)
                {
                    var currentDate = new DateTime(year, month, day);
                    var rule = interestRules.LastOrDefault(ir => ir.Key <= currentDate);
                    if (rule.Value != null)
                    {
                        // Apply interest for the previous days balance
                        totalInterest += (balance * (rule.Value.Rate / 100) / 365);
                    }
                }

                var finalInterest = Math.Round(totalInterest, 2);
                balance += finalInterest;

                Console.WriteLine($"| {endOfMonth:yyyyMMdd} |             | I    | {finalInterest,6:F2} | {balance,8:F2} |");
            }
        }

        private void PrintAccountStatement(string accountId)
        {
            Console.WriteLine($"Account: {accountId}");
            Console.WriteLine("| Date     | Txn Id      | Type | Amount | Balance |");

            decimal balance = 0;
            foreach (var transaction in accounts[accountId].Transactions)
            {
                balance += transaction.Type == 'D' ? transaction.Amount : -transaction.Amount;
                Console.WriteLine($"| {transaction.Date:yyyyMMdd} | {transaction.Id} | {transaction.Type} | {transaction.Amount,6:F2} | {balance,8:F2} |");
            }
        }

        static void Main(string[] args)
        {
            var bankingSystem = new BankingSystem();
            bankingSystem.Run();
        }
    }
}
