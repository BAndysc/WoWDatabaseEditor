using System.Threading.Tasks;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Runners
{
    public abstract class CompoundProcessor<T, R1> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T>
    {
        private readonly R1 r1;

        protected CompoundProcessor(R1 r1)
        {
            this.r1 = r1;
        }

        public override void Initialize(ulong gameBuild)
        {
            r1.Initialize(gameBuild);
        }

        public bool PreProcess(ref readonly PacketHolder packet)
        {
            r1.Process(in packet);
            return true;
        }

        public async Task PostProcessFirstStep()
        {
            if (r1 is INeedToPostProcess p1)
                await p1.PostProcess();
        }
    }
    
    public abstract class CompoundProcessor<T, R1, R2> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T> where R2 : IPacketProcessor<T>
    {
        private readonly R1 r1;
        private readonly R2 r2;

        protected CompoundProcessor(R1 r1, R2 r2)
        {
            this.r1 = r1;
            this.r2 = r2;
        }

        public override void Initialize(ulong gameBuild)
        {
            r1.Initialize(gameBuild);
            r2.Initialize(gameBuild);
        }

        public bool PreProcess(ref readonly PacketHolder packet)
        {
            r1.Process(in packet);
            r2.Process(in packet);
            return true;
        }

        public async Task PostProcessFirstStep()
        {
            if (r1 is INeedToPostProcess p1)
                await p1.PostProcess();
            if (r2 is INeedToPostProcess p2)
                await p2.PostProcess();
        }
    }
    public abstract class CompoundProcessor<T, R1, R2, R3> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T> 
        where R2 : IPacketProcessor<T>
        where R3 : IPacketProcessor<T>
    {
        private readonly R1 r1;
        private readonly R2 r2;
        private readonly R3 r3;

        protected CompoundProcessor(R1 r1, R2 r2, R3 r3)
        {
            this.r1 = r1;
            this.r2 = r2;
            this.r3 = r3;
        }

        public override void Initialize(ulong gameBuild)
        {
            r1.Initialize(gameBuild);
            r2.Initialize(gameBuild);
            r3.Initialize(gameBuild);
        }

        public bool PreProcess(ref readonly PacketHolder packet)
        {
            r1.Process(in packet);
            r2.Process(in packet);
            r3.Process(in packet);
            return true;
        }

        public async Task PostProcessFirstStep()
        {
            if (r1 is INeedToPostProcess p1)
                await p1.PostProcess();
            if (r2 is INeedToPostProcess p2)
                await p2.PostProcess();
            if (r3 is INeedToPostProcess p3)
                await p3.PostProcess();
        }
    }
    
    public abstract class CompoundProcessor<T, R1, R2, R3, R4> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T> 
        where R2 : IPacketProcessor<T>
        where R3 : IPacketProcessor<T>
        where R4 : IPacketProcessor<T>
    {
        private readonly R1 r1;
        private readonly R2 r2;
        private readonly R3 r3;
        private readonly R4 r4;

        protected CompoundProcessor(R1 r1, R2 r2, R3 r3, R4 r4)
        {
            this.r1 = r1;
            this.r2 = r2;
            this.r3 = r3;
            this.r4 = r4;
        }
        
        public override void Initialize(ulong gameBuild)
        {
            r1.Initialize(gameBuild);
            r2.Initialize(gameBuild);
            r3.Initialize(gameBuild);
            r4.Initialize(gameBuild);
        }

        public bool PreProcess(ref readonly PacketHolder packet)
        {
            r1.Process(in packet);
            r2.Process(in packet);
            r3.Process(in packet);
            r4.Process(in packet);
            return true;
        }

        public async Task PostProcessFirstStep()
        {
            if (r1 is INeedToPostProcess p1)
                await p1.PostProcess();
            if (r2 is INeedToPostProcess p2)
                await p2.PostProcess();
            if (r3 is INeedToPostProcess p3)
                await p3.PostProcess();
            if (r4 is INeedToPostProcess p4)
                await p4.PostProcess();
        }
    }
    
    public abstract class CompoundProcessor<T, R1, R2, R3, R4, R5> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T> 
        where R2 : IPacketProcessor<T>
        where R3 : IPacketProcessor<T>
        where R4 : IPacketProcessor<T>
        where R5 : IPacketProcessor<T>
    {
        private readonly R1 r1;
        private readonly R2 r2;
        private readonly R3 r3;
        private readonly R4 r4;
        private readonly R5 r5;

        protected CompoundProcessor(R1 r1, R2 r2, R3 r3, R4 r4, R5 r5)
        {
            this.r1 = r1;
            this.r2 = r2;
            this.r3 = r3;
            this.r4 = r4;
            this.r5 = r5;
        }

        public override void Initialize(ulong gameBuild)
        {
            r1.Initialize(gameBuild);
            r2.Initialize(gameBuild);
            r3.Initialize(gameBuild);
            r4.Initialize(gameBuild);
            r5.Initialize(gameBuild);
        }
        
        public bool PreProcess(ref readonly PacketHolder packet)
        {
            r1.Process(in packet);
            r2.Process(in packet);
            r3.Process(in packet);
            r4.Process(in packet);
            r5.Process(in packet);
            return true;
        }

        public async Task PostProcessFirstStep()
        {
            if (r1 is INeedToPostProcess p1)
                await p1.PostProcess();
            if (r2 is INeedToPostProcess p2)
                await p2.PostProcess();
            if (r3 is INeedToPostProcess p3)
                await p3.PostProcess();
            if (r4 is INeedToPostProcess p4)
                await p4.PostProcess();
            if (r5 is INeedToPostProcess p5)
                await p5.PostProcess();
        }
    }
    
    public abstract class CompoundProcessor<T, R1, R2, R3, R4, R5, R6> : PacketProcessor<T>, ITwoStepPacketBoolProcessor where R1 : IPacketProcessor<T> 
        where R2 : IPacketProcessor<T>
        where R3 : IPacketProcessor<T>
        where R4 : IPacketProcessor<T>
        where R5 : IPacketProcessor<T>
        where R6 : IPacketProcessor<T>
    {
        private readonly R1 r1;
        private readonly R2 r2;
        private readonly R3 r3;
        private readonly R4 r4;
        private readonly R5 r5;
        private readonly R6 r6;

        protected CompoundProcessor(R1 r1, R2 r2, R3 r3, R4 r4, R5 r5, R6 r6)
        {
            this.r1 = r1;
            this.r2 = r2;
            this.r3 = r3;
            this.r4 = r4;
            this.r5 = r5;
            this.r6 = r6;
        }
        
        public override void Initialize(ulong gameBuild)
        {
            r1.Initialize(gameBuild);
            r2.Initialize(gameBuild);
            r3.Initialize(gameBuild);
            r4.Initialize(gameBuild);
            r5.Initialize(gameBuild);
            r6.Initialize(gameBuild);
        }

        public bool PreProcess(ref readonly PacketHolder packet)
        {
            r1.Process(in packet);
            r2.Process(in packet);
            r3.Process(in packet);
            r4.Process(in packet);
            r5.Process(in packet);
            r6.Process(in packet);
            return true;
        }

        public async Task PostProcessFirstStep()
        {
            if (r1 is INeedToPostProcess p1)
                await p1.PostProcess();
            if (r2 is INeedToPostProcess p2)
                await p2.PostProcess();
            if (r3 is INeedToPostProcess p3)
                await p3.PostProcess();
            if (r4 is INeedToPostProcess p4)
                await p4.PostProcess();
            if (r5 is INeedToPostProcess p5)
                await p5.PostProcess();
            if (r6 is INeedToPostProcess p6)
                await p6.PostProcess();
        }
    }
}