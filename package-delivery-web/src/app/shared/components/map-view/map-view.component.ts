import { AfterViewInit, Component, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import * as L from 'leaflet';

export interface MapMarker {
  id: string;
  lat: number;
  lng: number;
  label?: string;
  iconColor?: 'brand' | 'accent' | 'success';
}

// Leaflet's default marker icon URLs are relative and break under bundlers unless re-pointed to CDN assets.
const DEFAULT_ICON = L.icon({
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41]
});

@Component({
  selector: 'app-map-view',
  template: `<div #mapEl class="w-full h-full min-h-[16rem] rounded-lg z-0"></div>`
})
export class MapViewComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('mapEl', { static: true }) private readonly mapEl!: ElementRef<HTMLDivElement>;

  @Input() markers: MapMarker[] = [];
  /** Optional polyline connecting points in order, e.g. a planned delivery route. */
  @Input() routePoints: Array<{ lat: number; lng: number }> = [];
  @Input() zoom = 12;
  @Output() markerClick = new EventEmitter<MapMarker>();

  private map: L.Map | null = null;
  private markerLayer: L.LayerGroup | null = null;
  private routeLine: L.Polyline | null = null;
  private viewInitialized = false;

  ngAfterViewInit(): void {
    this.map = L.map(this.mapEl.nativeElement, { attributionControl: true }).setView([0, 0], 2);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors'
    }).addTo(this.map);

    this.markerLayer = L.layerGroup().addTo(this.map);
    this.viewInitialized = true;
    this.render();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.viewInitialized && (changes['markers'] || changes['routePoints'])) {
      this.render();
    }
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  private render(): void {
    if (!this.map || !this.markerLayer) return;

    this.markerLayer.clearLayers();
    this.routeLine?.remove();

    this.markers.forEach((marker) => {
      const leafletMarker = L.marker([marker.lat, marker.lng], { icon: DEFAULT_ICON });
      if (marker.label) leafletMarker.bindPopup(marker.label);
      leafletMarker.on('click', () => this.markerClick.emit(marker));
      leafletMarker.addTo(this.markerLayer!);
    });

    if (this.routePoints.length > 1) {
      this.routeLine = L.polyline(
        this.routePoints.map((p) => [p.lat, p.lng] as [number, number]),
        { color: '#f97316', weight: 3 }
      ).addTo(this.map);
    }

    const points = [...this.markers.map((m) => [m.lat, m.lng] as [number, number]), ...this.routePoints.map((p) => [p.lat, p.lng] as [number, number])];
    if (points.length === 1) {
      this.map.setView(points[0], this.zoom);
    } else if (points.length > 1) {
      this.map.fitBounds(L.latLngBounds(points), { padding: [32, 32] });
    }
  }
}
