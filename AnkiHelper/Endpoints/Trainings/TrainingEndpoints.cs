using AnkiHelper.Endpoints.Trainings;

namespace AnkiHelper.Endpoints.Trainings
{
    public static class TrainingEndpoints
    {
        public static void MapTrainingEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapWordOrder();
            app.MapSentenceComposition();
            app.MapTranslationSentence();
            app.MapDailyTrainingStatistics();
        }
    }
}
