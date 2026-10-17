using SimpleHMS.Data;
using Xunit;

namespace SimpleHMS.Tests.Unit
{
    // GO-batch splitter used by DatabaseManager.ProvisionDatabase. Pure string
    // logic — no database involved.
    public class DatabaseManagerBatchTests
    {
        [Fact]
        public void SplitBatches_SplitsAtExcessGoLinesAndKeepsFinalBatch()
        {
            IEnumerable<string> batches = DatabaseManager.SplitBatches(
                "SELECT 1;\r\nSELECT 2;\r\nGO\r\nSELECT 3;\r\nGO");

            string[] all = batches.ToArray();

            Assert.Equal(3, all.Length);   // two GO lines + trailing third batch
            Assert.Contains("SELECT 1;", all[0].Replace("\r\n", "\n"));
            Assert.Contains("SELECT 2;", all[0].Replace("\r\n", "\n"));
            Assert.DoesNotContain("GO", all[0].Replace("\r\n", "\n"));
            Assert.Contains("SELECT 3;", all[1].Replace("\r\n", "\n"));
        }

        [Fact]
        public void SplitBatches_MatchesGoCaseInsensitivelyAndIgnoresWhitespace()
        {
            IEnumerable<string> batches = DatabaseManager.SplitBatches(
                "SELECT 1;\r\n   go  \r\nSELECT 2;");

            Assert.Equal(2, batches.Count());
        }

        [Fact]
        public void SplitBatches_OnlySplitsWholeLineGo()
        {
            // "GO 10" is not a standalone GO marker, so it must survive inside
            // one batch (the script executor treats it as SQL text).
            IEnumerable<string> batches = DatabaseManager.SplitBatches(
                "EXEC sp_some_procedure;\r\nGO 10\r\nSELECT 1;");

            string[] all = batches.ToArray();

            Assert.Single(all);
            Assert.Contains("GO 10", all[0].Replace("\r\n", "\n"));
        }

        [Fact]
        public void SplitBatches_PreservesMultipartSqlStatements()
        {
            string sql =
                "IF DB_ID(N'Demo') IS NULL CREATE DATABASE [Demo];\r\n" +
                "GO\r\n" +
                "USE [Demo];\r\n" +
                "CREATE TABLE dbo.T (A INT);\r\n" +
                "GO\r\n";

            string[] all = DatabaseManager.SplitBatches(sql).ToArray();

            Assert.Equal(3, all.Length);
            Assert.Equal("USE [Demo];\nCREATE TABLE dbo.T (A INT);\n",
                all[1].Replace("\r\n", "\n"));
        }

        [Fact]
        public void SplitBatches_HandlesUnixLineEndings()
        {
            string[] all = DatabaseManager.SplitBatches("SELECT 1;\nGO\nSELECT 2;").ToArray();

            Assert.Equal(2, all.Length);
        }
    }
}
