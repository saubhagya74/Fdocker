using System;
using IdGen;
    public class SnowFlakeGen
    {
        private readonly IdGenerator _idGenerator;

        public SnowFlakeGen()
        {
            // Generator ID (0–1023)
            var generatorId = 1;

            // Optional: Custom epoch and structure
            var epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var structure = new IdStructure(41, 10, 12); // Default layout
            var options = new IdGeneratorOptions(structure, new DefaultTimeSource(epoch));

            // NEW constructor for IdGen v3.0.7
            _idGenerator = new IdGenerator(generatorId, options);
        }

        public long GenerateId()
        {
            return _idGenerator.CreateId();
        }
    }
