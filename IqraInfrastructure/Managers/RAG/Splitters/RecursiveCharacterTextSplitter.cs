using IqraCore.Interfaces.RAG;

namespace IqraInfrastructure.Managers.RAG.Splitters
{
    public class RecursiveCharacterTextSplitter : ITextSplitter
    {
        private readonly int _chunkSize;
        private readonly int _chunkOverlap;
        private readonly string _separator;

        public RecursiveCharacterTextSplitter(int chunkSize = 1024, int chunkOverlap = 50, string separator = "\n")
        {
            if (chunkOverlap > chunkSize)
            {
                throw new ArgumentException($"Chunk overlap ({chunkOverlap}) cannot be larger than chunk size ({chunkSize}).");
            }
            _chunkSize = chunkSize;
            _chunkOverlap = chunkOverlap;
            _separator = separator;
        }

        public List<string> SplitText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string>();
            }

            List<string> newChunks = new List<string>();

            if (text.Length < _chunkSize)
            {
                newChunks.Add(text);
            }
            else
            {
                // Split the chunk by the current separator
                var splits = _separator == ""
                    ? text.Select(c => c.ToString()).ToList()
                    : text.Split(new[] { _separator }, StringSplitOptions.None).ToList();

                for (int i = 0; i < splits.Count; i++)
                {
                    // Re-add the separator to all but the last split part
                    if (_separator != "" && i < splits.Count - 1)
                    {
                        splits[i] += _separator;
                    }

                    if (!string.IsNullOrEmpty(splits[i]))
                    {
                        newChunks.Add(splits[i]);
                    }
                }
            }

            return MergeChunks(newChunks);
        }

        private List<string> MergeChunks(List<string> smallChunks)
        {
            var finalChunks = new List<string>();
            var currentChunkParts = new List<string>();
            var currentLength = 0;

            foreach (var part in smallChunks)
            {
                if (currentLength + part.Length <= _chunkSize)
                {
                    currentChunkParts.Add(part);
                    currentLength += part.Length;
                }
                else
                {
                    // Create the chunk from the current parts
                    finalChunks.Add(string.Concat(currentChunkParts));

                    // Start a new chunk, but respect the overlap
                    var overlapParts = new List<string>();
                    var overlapLength = 0;

                    // Go backwards from the current parts to build the overlap
                    for (int i = currentChunkParts.Count - 1; i >= 0; i--)
                    {
                        if (overlapLength + currentChunkParts[i].Length > _chunkOverlap) break;
                        overlapParts.Insert(0, currentChunkParts[i]);
                        overlapLength += currentChunkParts[i].Length;
                    }

                    // The new chunk starts with the overlap and the current part
                    currentChunkParts = overlapParts;
                    currentChunkParts.Add(part);
                    currentLength = overlapLength + part.Length;

                    // If a single part is larger than the chunk size, we must add it as-is
                    while (currentLength > _chunkSize)
                    {
                        var lastPart = currentChunkParts.Last();
                        currentChunkParts.RemoveAt(currentChunkParts.Count - 1);
                        finalChunks.Add(string.Concat(currentChunkParts));

                        currentChunkParts = new List<string> { lastPart };
                        currentLength = lastPart.Length;
                    }
                }
            }

            // Add the last remaining chunk
            if (currentChunkParts.Any())
            {
                finalChunks.Add(string.Concat(currentChunkParts));
            }

            return finalChunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        }
    }
}
