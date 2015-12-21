using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Infragistics.Documents.Excel;

namespace PeaceWalkerTools
{
    static class ExcelUtility
    {
        private static Dictionary<string, string> SPECIAL_LETTERS = new Dictionary<string, string>
        {
            {"“","{*"},
            {"”","*}"},
            {"「","["},
            {"」","]"},
            {"『","{"},
            {"』", "}"},
        };

        private static Dictionary<string, string> SPECIAL_LETTERS_BACK = new Dictionary<string, string>
        {
            {@"{*","“"},
            {@"*}","”"},
            {@"[" ,"「"},
            {@"]" ,"」"},
            {@"{" ,"『"},
            {@"}","』"},
        };

        private static string EXPRESSION = string.Format("({0})", string.Join("|", SPECIAL_LETTERS.Keys));
        private static string EXPRESSION_BACK = string.Format("({0})", string.Join("|", SPECIAL_LETTERS_BACK.Keys.Select(x => Regex.Escape(x))));


        static string MultipleReplace(this string text, string expression, Dictionary<string, string> replacements)
        {
            return Regex.Replace(text, expression, m => replacements[m.Value]);
        }

        public static void ReplaceSpecialLetter(string path, params int[] columns)
        {
            Replace(path, columns, EXPRESSION, SPECIAL_LETTERS);
        }

        public static void ReplaceBackSpecialLetter(string path, params int[] columns)
        {
            Replace(path, columns, EXPRESSION_BACK, SPECIAL_LETTERS_BACK);
        }

        private static void Replace(string path, int[] columns, string expression, Dictionary<string, string> dic)
        {
            var workbook = Workbook.Load(path);

            var sheet = workbook.Worksheets.First();

            var rowIndex = 1;

            while (true)
            {
                var row = sheet.Rows[rowIndex++];

                if (row.Cells[0].Value == null)
                {
                    break;
                }

                for (int i = 0; i < columns.Length; i++)
                {
                    var text = row.Cells[columns[i]].GetText();
                    row.Cells[columns[i]].Value = text.MultipleReplace(expression, dic);
                }
            }
            workbook.Save(path);
        }
    }
}
