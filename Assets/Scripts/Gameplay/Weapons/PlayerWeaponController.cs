using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InterrogationRoom.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class PlayerWeaponController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform weaponSocket;
        [SerializeField] private GameObject heldWeaponVisualPrefab;
        [SerializeField] private AudioClip shotSound;

        [Header("Shooting")]
        [SerializeField, Min(1f)] private float shotRange = 100f;
        [SerializeField, Min(0.05f)] private float shotInterval = 0.25f;
        [SerializeField, Min(1f)] private float maxShotOriginDistance = 10f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Feedback")]
        [SerializeField, Min(0.001f)] private float tracerWidth = 0.02f;
        [SerializeField, Min(0.01f)] private float tracerLifetime = 0.12f;
        [SerializeField, Min(0.05f)] private float tracerLength = 1.25f;
        [SerializeField, ColorUsage(true, true)] private Color tracerColor = new Color(1f, 0.75f, 0.2f, 1f);
        [SerializeField, Range(0f, 1f)] private float shotSoundVolume = 1f;

        [SyncVar(hook = nameof(OnHasWeaponChanged))]
        private bool hasWeapon;

        private double nextServerShotTime;
        private GameObject heldWeaponVisual;
        private WeaponMuzzle heldWeaponMuzzle;
        private Material tracerMaterial;

        public bool HasWeapon => hasWeapon;

        public override void OnStartClient()
        {
            base.OnStartClient();
            RefreshHeldWeaponVisual(hasWeapon);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            if (playerCamera == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerWeaponController)}] The local player requires a child Camera.",
                    this);
            }

            if (heldWeaponVisualPrefab == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerWeaponController)}] Assign a held weapon visual prefab before testing pickup.",
                    this);
            }
            else if (!heldWeaponVisualPrefab.TryGetComponent(out WeaponMuzzle muzzle) || !muzzle.IsConfigured)
            {
                Debug.LogError(
                    $"[{nameof(PlayerWeaponController)}] The held weapon visual requires a configured {nameof(WeaponMuzzle)}.",
                    this);
            }

            if (weaponSocket == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerWeaponController)}] Assign an explicit WeaponSocket before testing pickup.",
                    this);
            }

            if (shotSound == null)
            {
                Debug.LogError(
                    $"[{nameof(PlayerWeaponController)}] Assign a gunshot AudioClip before testing firing.",
                    this);
            }
        }

        public override void OnStopClient()
        {
            ClearHeldWeaponVisual();
            base.OnStopClient();
        }

        private void Update()
        {
            if (!isLocalPlayer || playerCamera == null)
            {
                return;
            }

            if (hasWeapon && WasFirePressed())
            {
                Transform cameraTransform = playerCamera.transform;
                CmdTryFire(cameraTransform.position, cameraTransform.forward);
            }
        }

        [Server]
        public bool TryEquipWeaponServer()
        {
            if (hasWeapon || !NetworkServer.active)
            {
                return false;
            }

            hasWeapon = true;
            return true;
        }

        [Command]
        private void CmdTryFire(Vector3 requestedOrigin, Vector3 requestedDirection)
        {
            if (!hasWeapon ||
                !IsFinite(requestedOrigin) ||
                !IsFinite(requestedDirection) ||
                requestedDirection.sqrMagnitude < 0.5f)
            {
                return;
            }

            // The client aims through its camera (the crosshair sits on the
            // camera's centre ray), so the shot must start at the camera. The
            // camera position is client authored; only accept it near the
            // server-side body so a tampered client cannot shoot from afar.
            if ((requestedOrigin - transform.position).sqrMagnitude >
                maxShotOriginDistance * maxShotOriginDistance)
            {
                return;
            }

            double now = NetworkTime.time;
            if (now < nextServerShotTime)
            {
                return;
            }

            nextServerShotTime = now + shotInterval;

            Vector3 direction = requestedDirection.normalized;
            ShotResolution shot = ResolveShot(requestedOrigin, direction);
            RpcShowShot(requestedOrigin, shot.Endpoint, shot.Normal, shot.HitKind);
        }

        [Server]
        private ShotResolution ResolveShot(Vector3 origin, Vector3 direction)
        {
            Vector3 endpoint = origin + direction * shotRange;
            Vector3 hitNormal = -direction;
            float closestDistance = shotRange;
            Collider closestCollider = null;
            IShotHitReceiver closestReceiver = null;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                shotRange,
                hitMask,
                QueryTriggerInteraction.Collide);

            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                {
                    continue;
                }

                IShotHitReceiver receiver = hit.collider.GetComponentInParent(typeof(IShotHitReceiver))
                    as IShotHitReceiver;
                if (hit.collider.isTrigger && receiver == null)
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    endpoint = hit.point;
                    hitNormal = hit.normal;
                    closestCollider = hit.collider;
                    closestReceiver = receiver;
                }
            }

            if (closestCollider == null)
            {
                return new ShotResolution(endpoint, hitNormal, ShotHitKind.Miss);
            }

            ShotHitKind hitKind = closestReceiver?.HitKind ?? ShotHitKind.Surface;
            closestReceiver?.ReceiveShotServer(new ShotHitContext(
                netIdentity,
                closestCollider,
                endpoint,
                hitNormal,
                direction));

            return new ShotResolution(endpoint, hitNormal, hitKind);
        }

        [ClientRpc]
        private void RpcShowShot(
            Vector3 origin,
            Vector3 endpoint,
            Vector3 hitNormal,
            ShotHitKind hitKind)
        {
            Vector3 visualOrigin = heldWeaponMuzzle != null ? heldWeaponMuzzle.Position : origin;
            heldWeaponMuzzle?.PlayFlash();
            SpawnTracer(visualOrigin, endpoint);
            ShotImpactEffect.Spawn(endpoint, hitNormal, hitKind);

            if (shotSound != null)
            {
                AudioSource.PlayClipAtPoint(shotSound, visualOrigin, shotSoundVolume);
            }
        }

        private void SpawnTracer(Vector3 origin, Vector3 endpoint)
        {
            GameObject tracerObject = new("Shot Tracer");
            ShotTracer tracer = tracerObject.AddComponent<ShotTracer>();
            tracer.Initialize(
                origin,
                endpoint,
                GetTracerMaterial(),
                tracerColor,
                tracerWidth,
                tracerLifetime,
                tracerLength);
        }

        private Material GetTracerMaterial()
        {
            if (tracerMaterial != null)
            {
                return tracerMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                return null;
            }

            tracerMaterial = new Material(shader)
            {
                color = tracerColor
            };
            return tracerMaterial;
        }

        private void OnHasWeaponChanged(bool _, bool newValue)
        {
            if (!isClient)
            {
                return;
            }

            RefreshHeldWeaponVisual(newValue);
        }

        private void RefreshHeldWeaponVisual(bool shouldShow)
        {
            ClearHeldWeaponVisual();

            if (!shouldShow || heldWeaponVisualPrefab == null || weaponSocket == null)
            {
                return;
            }

            heldWeaponVisual = Instantiate(heldWeaponVisualPrefab, weaponSocket);
            heldWeaponMuzzle = heldWeaponVisual.GetComponent<WeaponMuzzle>();
        }

        private void ClearHeldWeaponVisual()
        {
            if (heldWeaponVisual != null)
            {
                Destroy(heldWeaponVisual);
                heldWeaponVisual = null;
            }

            heldWeaponMuzzle = null;
        }

        private void OnDestroy()
        {
            if (tracerMaterial != null)
            {
                Destroy(tracerMaterial);
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool WasFirePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private readonly struct ShotResolution
        {
            public ShotResolution(Vector3 endpoint, Vector3 normal, ShotHitKind hitKind)
            {
                Endpoint = endpoint;
                Normal = normal;
                HitKind = hitKind;
            }

            public Vector3 Endpoint { get; }
            public Vector3 Normal { get; }
            public ShotHitKind HitKind { get; }
        }
    }
}
