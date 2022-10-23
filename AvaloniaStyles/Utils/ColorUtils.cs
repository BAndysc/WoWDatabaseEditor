using System;
using System.Numerics;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace AvaloniaStyles.Utils;

// Source: https://bottosson.github.io/misc/colorpicker/#4493ff
public static class ColorUtils
{
    internal struct Double2
    {
        public double X;
        public double Y;

        public Double2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Double2()
        {
            X = 0;
            Y = 0;
        }

        public double this[int index]
        {
            get
            {
                if (index == 0)
                    return X;
                if (index == 1)
                    return Y;
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (index == 0)
                    X = value;
                else if (index == 1)
                    Y = value;
                else
                    throw new IndexOutOfRangeException();
            }
        }
    }

    public struct Double3
    {
        public double X;
        public double Y;
        public double Z;

        public Double3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Double3()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public double this[int index]
        {
            get
            {
                if (index == 0)
                    return X;
                if (index == 1)
                    return Y;
                if (index == 2)
                    return Z;
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (index == 0)
                    X = value;
                else if (index == 1)
                    Y = value;
                else if (index == 2)
                    Z = value;
                else
                    throw new IndexOutOfRangeException();
            }
        }
    }

    private static double srgb_transfer_function(double a)
    {
        return .0031308 >= a ? 12.92 * a : 1.055 * Math.Pow(a, .4166666666666667) - .055;
    }

    private static double srgb_transfer_function_inv(double a)
    {
        return .04045 < a ? Math.Pow((a + .055) / 1.055, 2.4) : a / 12.92;
    }

    private static Double3 linear_srgb_to_oklab(double r, double g, double b)
    {
        double l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
        double m = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
        double s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

        double l_ = Math.Cbrt(l);
        double m_ = Math.Cbrt(m);
        double s_ = Math.Cbrt(s);

        return new Double3(
            0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_,
            1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_,
            0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_);
    }

    public static readonly Vector3 aConst = new Vector3(0.3963377774f, -0.1055613458f, -0.0894841775f);
    public static readonly Vector3 bConst = new Vector3(0.2158037573f, -0.0638541728f, -1.2914855480f);
    
    
    public static readonly Vector3 xConst = new Vector3(+4.0767416621f, - 3.3077115913f, 0.2309699292f);
    public static readonly Vector3 yConst = new Vector3(-1.2684380046f, + 2.6097574011f, - 0.3413193965f);
    public static readonly Vector3 zConst = new Vector3(-0.0041960863f, - 0.7034186147f, + 1.7076147010f);

    private static Double3 oklab_to_linear_srgb(double L, double a, double b)
    {
        Vector3 a_ = new Vector3((float)a, (float)a, (float)a);
        Vector3 b_ = new Vector3((float)b, (float)b, (float)b);
        
        Vector3 lms = new Vector3((float)L, (float)L, (float)L) + aConst * a_ + bConst * b_;
        //double l_ = L + 0.3963377774 * a + 0.2158037573 * b;
        //double m_ = L - 0.1055613458 * a - 0.0638541728 * b;
        //double s_ = L - 0.0894841775 * a - 1.2914855480 * b;

        lms = lms * lms * lms;
        
        //double l = l_ * l_ * l_;
        //double m = m_ * m_ * m_;
        //double s = s_ * s_ * s_;

        return new Double3(
            Vector3.Dot(xConst, lms), Vector3.Dot(yConst, lms), Vector3.Dot(zConst, lms));
            //(+4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s),
            //(-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s),
            //(-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s));
    }

    private static double toe(double x)
    {
        const double k_1 = 0.206;
        const double k_2 = 0.03;
        const double k_3 = (1 + k_1) / (1 + k_2);

        return 0.5 * (k_3 * x - k_1 + Math.Sqrt((k_3 * x - k_1) * (k_3 * x - k_1) + 4 * k_2 * k_3 * x));
    }

    private static double toe_inv(double x)
    {
        const double k_1 = 0.206;
        const double k_2 = 0.03;
        const double k_3 = (1 + k_1) / (1 + k_2);
        return (x * x + k_1 * x) / (k_3 * (x + k_2));
    }

// Finds the maximum saturation possible for a given hue that fits in sRGB
// Saturation here is defined as S = C/L
// a and b must be normalized so a^2 + b^2 == 1
    private static double compute_max_saturation(double a, double b)
    {
        // Max saturation will be when one of r, g or b goes below zero.

        // Select different coefficients depending on which component goes below zero first
        double k0, k1, k2, k3, k4, wl, wm, ws;

        if (-1.88170328 * a - 0.80936493 * b > 1)
        {
            // Red component
            k0 = +1.19086277;
            k1 = +1.76576728;
            k2 = +0.59662641;
            k3 = +0.75515197;
            k4 = +0.56771245;
            wl = +4.0767416621;
            wm = -3.3077115913;
            ws = +0.2309699292;
        }
        else if (1.81444104 * a - 1.19445276 * b > 1)
        {
            // Green component
            k0 = +0.73956515;
            k1 = -0.45954404;
            k2 = +0.08285427;
            k3 = +0.12541070;
            k4 = +0.14503204;
            wl = -1.2684380046;
            wm = +2.6097574011;
            ws = -0.3413193965;
        }
        else
        {
            // Blue component
            k0 = +1.35733652;
            k1 = -0.00915799;
            k2 = -1.15130210;
            k3 = -0.50559606;
            k4 = +0.00692167;
            wl = -0.0041960863;
            wm = -0.7034186147;
            ws = +1.7076147010;
        }

        // Approximate max saturation using a polynomial:
        double S = k0 + k1 * a + k2 * b + k3 * a * a + k4 * a * b;

        // Do one step Halley's method to get closer
        // this gives an error less than 10e6, except for some blue hues where the dS/dh is close to infinite
        // this should be sufficient for most applications, otherwise do two/three steps 

        double k_l = +0.3963377774 * a + 0.2158037573 * b;
        double k_m = -0.1055613458 * a - 0.0638541728 * b;
        double k_s = -0.0894841775 * a - 1.2914855480 * b;

        {
            double l_ = 1 + S * k_l;
            double m_ = 1 + S * k_m;
            double s_ = 1 + S * k_s;

            double l = l_ * l_ * l_;
            double m = m_ * m_ * m_;
            double s = s_ * s_ * s_;

            double l_dS = 3 * k_l * l_ * l_;
            double m_dS = 3 * k_m * m_ * m_;
            double s_dS = 3 * k_s * s_ * s_;

            double l_dS2 = 6 * k_l * k_l * l_;
            double m_dS2 = 6 * k_m * k_m * m_;
            double s_dS2 = 6 * k_s * k_s * s_;

            double f = wl * l + wm * m + ws * s;
            double f1 = wl * l_dS + wm * m_dS + ws * s_dS;
            double f2 = wl * l_dS2 + wm * m_dS2 + ws * s_dS2;

            S = S - f * f1 / (f1 * f1 - 0.5 * f * f2);
        }

        return S;
    }

    private static Double2 find_cusp(double a, double b)
    {
        // First, find the maximum saturation (saturation S = C/L)
        double S_cusp = compute_max_saturation(a, b);

        // Convert to linear sRGB to find the first point where at least one of r,g or b >= 1:
        Double3 rgb_at_max = oklab_to_linear_srgb(1, S_cusp * a, S_cusp * b);
        double L_cusp = Math.Cbrt(1 / Math.Max(Math.Max(rgb_at_max[0], rgb_at_max[1]), rgb_at_max[2]));
        double C_cusp = L_cusp * S_cusp;

        return new Double2(L_cusp, C_cusp);
    }

// Finds intersection of the line defined by 
// L = L0 * (1 - t) + t * L1;
// C = t * C1;
// a and b must be normalized so a^2 + b^2 == 1
    private static double find_gamut_intersection(double a, double b, double L1, double C1, double L0,
        Double2? cusp2 = null)
    {
        Double2 cusp;
        if (!cusp2.HasValue)
        {
            // Find the cusp of the gamut triangle
            cusp = find_cusp(a, b);
        }
        else
            cusp = cusp2.Value;

        // Find the intersection for upper and lower half seprately
        double t;
        if (((L1 - L0) * cusp[1] - (cusp[0] - L0) * C1) <= 0)
        {
            // Lower half

            t = cusp[1] * L0 / (C1 * cusp[0] + cusp[1] * (L0 - L1));
        }
        else
        {
            // Upper half

            // First intersect with triangle
            t = cusp[1] * (L0 - 1) / (C1 * (cusp[0] - 1) + cusp[1] * (L0 - L1));

            // Then one step Halley's method
            {
                double dL = L1 - L0;
                double dC = C1;

                double k_l = +0.3963377774 * a + 0.2158037573 * b;
                double k_m = -0.1055613458 * a - 0.0638541728 * b;
                double k_s = -0.0894841775 * a - 1.2914855480 * b;

                double l_dt = dL + dC * k_l;
                double m_dt = dL + dC * k_m;
                double s_dt = dL + dC * k_s;


                // If higher accuracy is required, 2 or 3 iterations of the following block can be used:
                {
                    double L = L0 * (1 - t) + t * L1;
                    double C = t * C1;

                    double l_ = L + C * k_l;
                    double m_ = L + C * k_m;
                    double s_ = L + C * k_s;

                    double l = l_ * l_ * l_;
                    double m = m_ * m_ * m_;
                    double s = s_ * s_ * s_;

                    double ldt = 3 * l_dt * l_ * l_;
                    double mdt = 3 * m_dt * m_ * m_;
                    double sdt = 3 * s_dt * s_ * s_;

                    double ldt2 = 6 * l_dt * l_dt * l_;
                    double mdt2 = 6 * m_dt * m_dt * m_;
                    double sdt2 = 6 * s_dt * s_dt * s_;

                    double r = 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s - 1;
                    double r1 = 4.0767416621 * ldt - 3.3077115913 * mdt + 0.2309699292 * sdt;
                    double r2 = 4.0767416621 * ldt2 - 3.3077115913 * mdt2 + 0.2309699292 * sdt2;

                    double u_r = r1 / (r1 * r1 - 0.5 * r * r2);
                    double t_r = -r * u_r;

                    double g = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s - 1;
                    double g1 = -1.2684380046 * ldt + 2.6097574011 * mdt - 0.3413193965 * sdt;
                    double g2 = -1.2684380046 * ldt2 + 2.6097574011 * mdt2 - 0.3413193965 * sdt2;

                    double u_g = g1 / (g1 * g1 - 0.5 * g * g2);
                    double t_g = -g * u_g;

                    // ??
                    b = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s - 1;
                    double b1 = -0.0041960863 * ldt - 0.7034186147 * mdt + 1.7076147010 * sdt;
                    double b2 = -0.0041960863 * ldt2 - 0.7034186147 * mdt2 + 1.7076147010 * sdt2;

                    double u_b = b1 / (b1 * b1 - 0.5 * b * b2);
                    double t_b = -b * u_b;

                    t_r = u_r >= 0 ? t_r : 10e5;
                    t_g = u_g >= 0 ? t_g : 10e5;
                    t_b = u_b >= 0 ? t_b : 10e5;

                    t += Math.Min(t_r, Math.Min(t_g, t_b));
                }
            }
        }

        return t;
    }

    private static Double2 get_ST_max(double a_, double b_, Double2? cusp2 = null)
    {
        Double2 cusp = cusp2 ?? find_cusp(a_, b_);

        double L = cusp[0];
        double C = cusp[1];
        return new Double2(C / L, C / (1 - L));
    }

    private static Double2 get_ST_mid(double a_, double b_)
    {
        double S = 0.11516993 + 1 / (
            +7.44778970 + 4.15901240 * b_
                        + a_ * (-2.19557347 + 1.75198401 * b_
                                            + a_ * (-2.13704948 - 10.02301043 * b_
                                                    + a_ * (-4.24894561 + 5.38770819 * b_ + 4.69891013 * a_
                                                    )))
        );

        double T = 0.11239642 + 1 / (
            +1.61320320 - 0.68124379 * b_
            + a_ * (+0.40370612 + 0.90148123 * b_
                                + a_ * (-0.27087943 + 0.61223990 * b_
                                                    + a_ * (+0.00299215 - 0.45399568 * b_ - 0.14661872 * a_
                                                    )))
        );

        return new Double2(S, T);
    }

    private static Double3 get_Cs(double L, double a_, double b_)
    {
        Double2 cusp = find_cusp(a_, b_);

        double C_max = find_gamut_intersection(a_, b_, L, 1, L, cusp);
        Double2 ST_max = get_ST_max(a_, b_, cusp);

        double S_mid = 0.11516993 + 1 / (
            +7.44778970 + 4.15901240 * b_
                        + a_ * (-2.19557347 + 1.75198401 * b_
                                            + a_ * (-2.13704948 - 10.02301043 * b_
                                                    + a_ * (-4.24894561 + 5.38770819 * b_ + 4.69891013 * a_
                                                    )))
        );

        double T_mid = 0.11239642 + 1 / (
            +1.61320320 - 0.68124379 * b_
            + a_ * (+0.40370612 + 0.90148123 * b_
                                + a_ * (-0.27087943 + 0.61223990 * b_
                                                    + a_ * (+0.00299215 - 0.45399568 * b_ - 0.14661872 * a_
                                                    )))
        );

        double k = C_max / Math.Min((L * ST_max[0]), (1 - L) * ST_max[1]);

        double C_mid;
        {
            double C_a = L * S_mid;
            double C_b = (1 - L) * T_mid;

            C_mid = 0.9 * k * Math.Sqrt(Math.Sqrt(1 / (1 / (C_a * C_a * C_a * C_a) + 1 / (C_b * C_b * C_b * C_b))));
        }

        double C_0;
        {
            double C_a = L * 0.4;
            double C_b = (1 - L) * 0.8;

            C_0 = Math.Sqrt(1 / (1 / (C_a * C_a) + 1 / (C_b * C_b)));
        }

        return new Double3(C_0, C_mid, C_max);
    }

    public static (double cosH, double sinH, double L, Double3 cs) precompute_okhsl_to_srgb(double h, double s, double l)
    {
        double a_ = Math.Cos(2 * Math.PI * h);
        double b_ = Math.Sin(2 * Math.PI * h);
        double L = toe_inv(l);

        Double3 Cs = get_Cs(L, a_, b_);
        return (a_, b_, L, Cs);
    }
    
    public static Double3 fast_okhsl_to_srgb(double h, double s, double l, double cosH, double sinH, double toeInvL, Double3 cs)
    {
        double a_ = cosH;
        double b_ = sinH;
        double L = toeInvL;
        Double3 Cs = cs;
        
        double C_0 = Cs[0];
        double C_mid = Cs[1];
        double C_max = Cs[2];

        double C, t, k_0, k_1, k_2;
        if (s < 0.8)
        {
            t = 1.25 * s;
            k_0 = 0;
            k_1 = 0.8 * C_0;
            k_2 = (1 - k_1 / C_mid);
        }
        else
        {
            t = 5 * (s - 0.8);
            k_0 = C_mid;
            k_1 = 0.2 * C_mid * C_mid * 1.25 * 1.25 / C_0;
            k_2 = (1 - (k_1) / (C_max - C_mid));
        }

        C = k_0 + t * k_1 / (1 - k_2 * t);

        // If we would only use one of the Cs:
        //C = s*C_0;
        //C = s*1.25*C_mid;
        //C = s*C_max;

        Double3 rgb = oklab_to_linear_srgb(L, C * a_, C * b_);
        return new Double3(
            255 * srgb_transfer_function(rgb[0]),
            255 * srgb_transfer_function(rgb[1]),
            255 * srgb_transfer_function(rgb[2])
        );
    }
    
    internal static Double3 okhsl_to_srgb(double h, double s, double l)
    {
        if (l == 1)
        {
            return new Double3(255, 255, 255);
        }

        else if (l == 0)
        {
            return new Double3(0, 0, 0);
        }

        var (a_, b_, L, Cs) = precompute_okhsl_to_srgb(h, s, l);

        return fast_okhsl_to_srgb(h, s, l, a_, b_, L, Cs);
    }

    internal static Double3 srgb_to_okhsl(double r, double g, double b)
    {
        Double3 lab = linear_srgb_to_oklab(
            srgb_transfer_function_inv(r / 255.0),
            srgb_transfer_function_inv(g / 255.0),
            srgb_transfer_function_inv(b / 255.0)
        );

        double C = Math.Sqrt(lab[1] * lab[1] + lab[2] * lab[2]);
        double a_ = lab[1] / C;
        double b_ = lab[2] / C;

        double L = lab[0];
        double h = 0.5 + 0.5 * Math.Atan2(-lab[2], -lab[1]) / Math.PI;

        Double3 Cs = get_Cs(L, a_, b_);
        double C_0 = Cs[0];
        double C_mid = Cs[1];
        double C_max = Cs[2];

        double s;
        if (C < C_mid)
        {
            double k_0 = 0;
            double k_1 = 0.8 * C_0;
            double k_2 = (1 - k_1 / C_mid);

            double t = (C - k_0) / (k_1 + k_2 * (C - k_0));
            s = t * 0.8;
        }
        else
        {
            double k_0 = C_mid;
            double k_1 = 0.2 * C_mid * C_mid * 1.25 * 1.25 / C_0;
            double k_2 = (1 - (k_1) / (C_max - C_mid));

            double t = (C - k_0) / (k_1 + k_2 * (C - k_0));
            s = 0.8 + 0.2 * t;
        }

        double l = toe(L);
        return new Double3(h, s, l);
    }


    internal static Double3 okhsv_to_srgb(double h, double s, double v)
    {
        double a_ = Math.Cos(2 * Math.PI * h);
        double b_ = Math.Sin(2 * Math.PI * h);

        Double2 ST_max = get_ST_max(a_, b_);
        double S_max = ST_max[0];
        double S_0 = 0.5;
        double T = ST_max[1];
        double k = 1 - S_0 / S_max;

        double L_v = 1 - s * S_0 / (S_0 + T - T * k * s);
        double C_v = s * T * S_0 / (S_0 + T - T * k * s);

        double L = v * L_v;
        double C = v * C_v;

        // to present steps along the way
        //L = v;
        //C = v*s*S_max;
        //L = v*(1 - s*S_max/(S_max+T));
        //C = v*s*S_max*T/(S_max+T);

        double L_vt = toe_inv(L_v);
        double C_vt = C_v * L_vt / L_v;

        double L_new = toe_inv(L); // * L_v/L_vt;
        C = C * L_new / L;
        L = L_new;

        Double3 rgb_scale = oklab_to_linear_srgb(L_vt, a_ * C_vt, b_ * C_vt);
        double scale_L = Math.Cbrt(1 / (Math.Max(rgb_scale[0], Math.Max(rgb_scale[1], Math.Max(rgb_scale[2], 0)))));

        // remove to see effect without rescaling
        L = L * scale_L;
        C = C * scale_L;

        Double3 rgb = oklab_to_linear_srgb(L, C * a_, C * b_);
        return new Double3(
            255 * srgb_transfer_function(rgb[0]),
            255 * srgb_transfer_function(rgb[1]),
            255 * srgb_transfer_function(rgb[2]));
    }

    internal static Double3 srgb_to_okhsv(double r, double g, double b)
    {
        Double3 lab = linear_srgb_to_oklab(
            srgb_transfer_function_inv(r / 255.0),
            srgb_transfer_function_inv(g / 255.0),
            srgb_transfer_function_inv(b / 255.0)
        );

        double C = Math.Sqrt(lab[1] * lab[1] + lab[2] * lab[2]);
        double a_ = lab[1] / C;
        double b_ = lab[2] / C;

        double L = lab[0];
        double h = 0.5 + 0.5 * Math.Atan2(-lab[2], -lab[1]) / Math.PI;

        Double2 ST_max = get_ST_max(a_, b_);
        double S_max = ST_max[0];
        double S_0 = 0.5;
        double T = ST_max[1];
        double k = 1 - S_0 / S_max;

        double t = T / (C + L * T);
        double L_v = t * L;
        double C_v = t * C;

        double L_vt = toe_inv(L_v);
        double C_vt = C_v * L_vt / L_v;

        Double3 rgb_scale = oklab_to_linear_srgb(L_vt, a_ * C_vt, b_ * C_vt);
        double scale_L = Math.Cbrt(1 / (Math.Max(rgb_scale[0], Math.Max(rgb_scale[1], Math.Max(rgb_scale[2], 0)))));

        L = L / scale_L;
        C = C / scale_L;

        C = C * toe(L) / L;
        L = toe(L);

        double v = L / L_v;
        double s = (S_0 + T) * C_v / ((T * S_0) + T * k * C_v);

        return new Double3(h, s, v);
    }
}