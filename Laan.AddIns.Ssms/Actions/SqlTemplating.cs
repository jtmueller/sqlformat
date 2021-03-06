using System;
using System.Linq;
using System.Collections.Generic;
using Laan.AddIns.Actions;
using Laan.AddIns.Core;

namespace Laan.AddIns.Ssms.Actions
{
    [MenuBarToolsMenu]
    public class SqlTemplating : DropDownList
    {
        private static Dictionary<string, string> _templates;

        static SqlTemplating()
        {
            var line = new string( '-', 80 );

            // temporary template listing - this will change to being user-config, loaded from a file..
            _templates = new Dictionary<string, string>() 
            { 
                // Blocks, Transaction control
                { "br",     "BEGIN TRAN\n\n\t|\n\nROLLBACK" },
                { "bc",     "BEGIN TRAN\n\n\t|\n\nCOMMIT" },
                { "btr",    "BEGIN TRAN\n\n\t|\n\nROLLBACK\n--COMMIT" },
                { "btc",    "BEGIN TRAN\n\n\t|\n\nCOMMIT\n--ROLLBACK" },
                { "be",     "BEGIN\n\n\t|\n\nEND" },
                { "bt",     "BEGIN TRAN" },
                { "rt",     "ROLLBACK" },
                { "ct",     "COMMIT" },

                // Statements
                { "ssf",    "SELECT * FROM |" },
                { "s",      "SELECT |" },
                { "sfw",    "SELECT *\nFROM |\nWHERE " },

                { "ufw",    "UPDATE \n   SET \nFROM |\nWHERE " },
                { "iv",     "INSERT INTO | ()\nVALUES ()" },
                { "is",     "INSERT INTO | ()\n\tSELECT * FROM" },
                { "isw",    "INSERT INTO | ()\n\tSELECT * FROM WHERE " },
                
                { "ft",     "FROM |" },
                { "wh",     "WHERE | = " },
                { "wi",     "WHERE |ID = " },

                // Joins
                { "j",      "JOIN |\n  ON " },
                { "lj",     "LEFT JOIN |\n       ON " },
                { "rj",     "RIGHT JOIN |\n        ON " },
                { "fj",     "FULL JOIN |\n       ON " },
                { "cj",     "CROSS JOIN |" },

                // Helpers
                { "ob",    "ORDER BY |" },
                { "gb",    "GROUP BY |" },
                { "hv",    "HAVING |" },
                { "isn",   "IS NULL" },
                { "isnn",  "IS NOT NULL" },
                { "ex",    "EXEC |" },
                { "exi",    "EXISTS(|)" },
                { "nex",   "NOT EXISTS(|)" },
                { "pr",    "PRINT '|'" },
                { "d",     "dbo.|" },
                { "--",    String.Format("{0}\n-- |\n{0}", line) },

                // Declarations
                { "di",     "DECLARE @| INT" },
                { "dv",     "DECLARE @| VARCHAR(MAX)" },
                { "dn",     "DECLARE @| NUMERIC(12,4)" },
                { "dm",     "DECLARE @| MONEY" },
                { "ddt",    "DECLARE @| DATETIME" },
                { "dt",     "DECLARE @| TIME" },
                { "dd",     "DECLARE @| DATE" },

                // Declarations with assignment
                { "die",    "DECLARE @| INT = " },
                { "dve",    "DECLARE @| VARCHAR(MAX) = ''" },
                { "dne",    "DECLARE @| NUMERIC(12,4) = " },
                { "dme",    "DECLARE @| MONEY = " },
                { "ddte",   "DECLARE @| DATETIME = " },
                { "dte",    "DECLARE @| TIME = " },
                { "dde",    "DECLARE @| DATE = " },

                // Case
                { "ca",     "CASE | WHEN END" },
                { "cw",     "CASE WHEN | THEN  END" },
                { "cwe",    "CASE WHEN | THEN  ELSE  END" },
                { "wt",     "WHEN | THEN " },

                // If
                { "ib",     "IF |\nBEGIN\n\n\t\n\nEND" },
                { "ibe",    "IF |\nBEGIN\n\n\t\n\nEND\nELSE\nBEGIN\n\n\t\n\nEND" }
            };
        }

        public SqlTemplating( AddIn addIn ) : base( addIn )
        {
            KeyName = "LaanSqlTemplating";
            DisplayName = "Insert Template";
            DescriptivePhrase = "Inserting Template";

            ButtonText = "Insert &Template";
            ToolTip = "Inserts a template at the cursor";
            ImageIndex = 59;
            KeyboardBinding = "Text Editor::`";
        }

        private Cursor DetermineCursorFromBar( IList<string> lines )
        {
            int column = 0;
            int row = 0;

            foreach ( string line in lines )
            {
                column = line.IndexOf( '|' );
                if ( column >= 0 )
                    break;
                row++;
            }

            // if no bar was found, just put the cursor at the last row and column of the replaced text
            if ( column == -1 )
            {
                column = lines.Last().Length;
                row = lines.Count() - 1;
            }
            return new Cursor( column, row );
        }

        public override bool CanExecute()
        {
            return (
                AddIn.IsCurrentDocumentExtension( "sql" )
                && 
                AddIn.CurrentSelection == ""
            );
        }

        public void Expand( string word )
        {
            try
            {
                AddIn.SelectCurrentWord();
                string padding = new string( ' ', 4 );
                var raw = _templates[ word ]
                    .Split( '\n' )
                    .Select( line => line.Replace( "\t", padding ) )
                    .ToList();

                string offset = new string( ' ', AddIn.Cursor.Column - 1 );

                // add lines, indenting as required, except the first
                var lines = new List<String>();
                lines.Add( raw.FirstOrDefault() );
                for ( int index = 1; index < raw.Count; index++ )
                    lines.Add( ( raw[ index] == "" ? "" : offset ) + raw[ index] );

                Cursor cursor = DetermineCursorFromBar( raw );
                if ( cursor.Row < lines.Count )
                    lines[ cursor.Row ] = lines[ cursor.Row ].Replace( "|", "" );

                AddIn.InsertText( String.Join( "\n", lines.ToArray() ) );
                AddIn.Cursor = cursor;
            }
            finally
            {
                AddIn.CancelSelection();
            }
        }

        protected override void ExecuteItem( Item item )
        {
            Expand( item.Name );
        }

        protected override IEnumerable<Item> GetItems()
        {
            var word = AddIn.CurrentWord;

            foreach ( var template in _templates.OrderBy( k => k.Key ) )
            {
                if ( template.Key.StartsWith( word ) )
                    yield return new Item() { Name = template.Key, Description = template.Value };
            }
        }
    }
}
