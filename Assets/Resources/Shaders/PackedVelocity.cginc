
#define packed_velocity_t float4

// Pack the velocity to write to R10G10B10A2_UNORM
packed_velocity_t PackVelocity( float3 Velocity ) {
    // Stretch dx,dy from [-64, 63.875] to [-512, 511] to [-0.5, 0.5) to [0, 1)
    // Velocity.xy = (0,0) must be representable.
    return float4(Velocity * float3(8, 8, 4096) / 1024.0 + 512 / 1023.0, 0);
}

// Unpack the velocity from R10G10B10A2_UNORM
float3 UnpackVelocity( packed_velocity_t Velocity ) {
    return (Velocity.xyz - 512.0 / 1023.0) * float3(1024, 1024, 2) / 8.0;
}