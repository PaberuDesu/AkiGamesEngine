using Microsoft.Xna.Framework.Content.Pipeline;

namespace AkiContentPipeline
{
    [ContentImporter(".aki", DisplayName = "Aki Importer", DefaultProcessor = "AkiProcessor")]
    public class AkiImporter : ContentImporter<string>
    {
        public override string Import(string filename, ContentImporterContext context)
        {
            context.Logger.LogMessage("Importing AKI file: {0}", filename);
            return File.ReadAllText(filename);
        }
    }
}
