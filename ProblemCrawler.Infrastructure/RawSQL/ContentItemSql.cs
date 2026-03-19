namespace ProblemCrawler.Infrastructure.RawSQL
{
    public static class ContentItemSql
    {
        public const string insertionSql = ("""
                    INSERT INTO "CollectorItems"
                    (
                        "Id","SourceId","Source","ItemType",
                        "SelfText","Content","ParentId","LinkId",
                        "Metadata","CreatedAt","Author","SourceUrl","AnalysisStage"
                    )
                    VALUES
                    """);
        public const string conflictUpdateSql = ("""
                    ON CONFLICT ("SourceId","Source")
                    DO UPDATE SET
                        "SelfText" = EXCLUDED."SelfText",
                        "Content" = EXCLUDED."Content",
                        "ParentId" = EXCLUDED."ParentId",
                        "LinkId" = EXCLUDED."LinkId",
                        "Metadata" = EXCLUDED."Metadata",
                        "Author" = EXCLUDED."Author",
                        "SourceUrl" = EXCLUDED."SourceUrl",
                        "AnalysisStage" = 'New'
                    WHERE
                    (
                        "CollectorItems"."SelfText",
                        "CollectorItems"."Content",
                        "CollectorItems"."ParentId",
                        "CollectorItems"."LinkId",
                        "CollectorItems"."Author",
                        "CollectorItems"."SourceUrl"
                    )
                    IS DISTINCT FROM
                    (
                        EXCLUDED."SelfText",
                        EXCLUDED."Content",
                        EXCLUDED."ParentId",
                        EXCLUDED."LinkId",
                        EXCLUDED."Author",
                        EXCLUDED."SourceUrl"
                    );
                 """);
    }
}
