using System;

namespace WDE.MVVM.Utils
{
    public class CompositeDisposable : System.IDisposable
    {
        private readonly IDisposable disp1;
        private readonly IDisposable disp2;
        private readonly IDisposable? disp3;

        public CompositeDisposable(System.IDisposable disp1, System.IDisposable disp2, System.IDisposable? disp3 = null)
        {
            this.disp1 = disp1;
            this.disp2 = disp2;
            this.disp3 = disp3;
        }
        
        public void Dispose()
        {
            disp1.Dispose();
            disp2.Dispose();
            disp3?.Dispose();
        }
    }

    public static class DisposablesExtensions
    {
        public static IDisposable Combine(this IDisposable a, IDisposable b)
        {
            return new CompositeDisposable(a, b);
        }
    }
}