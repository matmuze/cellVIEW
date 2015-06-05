 //--------------------------------------------------------------------------------------

float Epsilon = 1e-10;

float3 RGBtoHCV(in float3 RGB)
{
	// Based on work by Sam Hocevar and Emil Persson
	float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0/3.0) : float4(RGB.gb, 0.0, -1.0/3.0);
	float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
	float C = Q.x - min(Q.w, Q.y);
	float H = abs((Q.w - Q.y) / (6 * C + Epsilon) + Q.z);
	return float3(H, C, Q.x);
}

float3 HUEtoRGB(in float H)
{
	float R = abs(H * 6 - 3) - 1;
	float G = 2 - abs(H * 6 - 2);
	float B = 2 - abs(H * 6 - 4);
	return saturate(float3(R,G,B));
}

//--------------------------------------------------------------------------------------

float3 RGBtoHSL(in float3 RGB)
{
	float3 HCV = RGBtoHCV(RGB);
	float L = HCV.z - HCV.y * 0.5;
	float S = HCV.y / (1 - abs(L * 2 - 1) + Epsilon);
	return float3(HCV.x, S, L);
}

float3 HSLtoRGB(in float3 HSL)
{
	float3 RGB = HUEtoRGB(HSL.x);
	float C = (1 - abs(2 * HSL.z - 1)) * HSL.y;
	return (RGB - 0.5) * C + HSL.z;
}

//--------------------------------------------------------------------------------------

float3 RGBtoHSV(in float3 RGB)
{
	float3 HCV = RGBtoHCV(RGB);
	float S = HCV.y / (HCV.z + Epsilon);
	return float3(HCV.x, S, HCV.z);
}

float3 HSVtoRGB(in float3 HSV)
{
	float3 RGB = HUEtoRGB(HSV.x);
	return ((RGB - 1) * HSV.y + 1) * HSV.z;
}

//--------------------------------------------------------------------------------------

float3 SetHSV(float3 color, float3 hsv)
{
	float3 c = RGBtoHSV(color);		
		
	c.x = (hsv.x < 0) ? c.x : hsv.x;
	c.y = (hsv.y < 0) ? c.y : hsv.y;
	c.z = (hsv.z < 0) ? c.z : hsv.z;

	return 	HSVtoRGB(c);	
}

float3 OffsetHSV(float3 color, float3 offset)
{
	float3 c = RGBtoHSV(color);		
	return 	HSVtoRGB(c + offset);	
}
		
//--------------------------------------------------------------------------------------

float3 ColorCorrection(float3 color)
{
	float3 c = RGBtoHSL(color);		
	
	c.z = 0.6;
	c.y = 0.6;

	return 	HSLtoRGB(c);	
}

float3 SetHSL(float3 color, float3 hsl)
{
	float3 c = RGBtoHSL(color);		
		
	c.x = (hsl.x < 0) ? c.x : hsl.x;
	c.y = (hsl.y < 0) ? c.y : hsl.y;
	c.z = (hsl.z < 0) ? c.z : hsl.z;

	return 	HSLtoRGB(c);		
}
	
float3 OffsetHSL(float3 color, float3 offset)
{
	float3 c = RGBtoHSL(color);		
	return 	HSLtoRGB(c + offset);		
}	

//--------------------------------------------------------------------------------------

float3 QuaternionTransform( float4 q, float3 v )
{ 
	float3 t = 2 * cross(q.xyz, v);
	return v + q.w * t + cross(q.xyz, t);
}

float4 QuaternionFromAxisAngle(float3 axis, float angle)
{
	float4 q;
	q.x = axis.x * sin(angle/2);
	q.y = axis.y * sin(angle/2);
	q.z = axis.z * sin(angle/2);
	q.w = cos(angle/2);
	return q;
}

float3 CubicInterpolate(float3 y0, float3 y1, float3 y2,float3 y3, float3 mu)
{
   float mu2 = mu*mu;
   float3 a0,a1,a2,a3;

   //a0 = -0.5*y0 + 1.5*y1 - 1.5*y2 + 0.5*y3;
   //a1 = y0 - 2.5*y1 + 2*y2 - 0.5*y3;
   //a2 = -0.5*y0 + 0.5*y2;
   //a3 = y1;

   a0 = y3 - y2 - y0 + y1;
   a1 = y0 - y1 - a0;
   a2 = y2 - y0;
   a3 = y1;

   return(a0*mu*mu2 + a1*mu2+a2 * mu+a3);
}

//*****//
		