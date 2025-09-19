using Microsoft.Xna.Framework.Content.Pipeline;

namespace AkiContentPipeline
{
    [ContentProcessor(DisplayName = "Aki Processor")]
    public class AkiProcessor : ContentProcessor<string, string>
    {
        public override string Process(string input, ContentProcessorContext context) => input;
    }
}