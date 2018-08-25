import { Directive, Input, ViewContainerRef, TemplateRef, OnInit } from '@angular/core';
import { AuthService } from '../_services/auth.service';

@Directive({
  selector: '[appHasRole]'
})
export class HasRoleDirective implements OnInit {

  @Input() appHasRole: string[];
  isVisible = false;
  constructor(
    private viewContainerRef: ViewContainerRef,
    private templateRed: TemplateRef<any>,
    private authService: AuthService
    ) { }

  ngOnInit(): void {
    const userRoles = this.authService.decodedToken.role as Array<string>;
    if (!userRoles) {
      this.viewContainerRef.clear();
      return;
    }

    if (this.authService.roleMatch(this.appHasRole)) {
      if (!this.isVisible) {
        this.isVisible = true;
        this.viewContainerRef.createEmbeddedView(this.templateRed);
      } else {
        this.isVisible = false;
        this.viewContainerRef.clear();
      }
    }
  }
}
