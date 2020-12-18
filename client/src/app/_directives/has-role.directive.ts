import {
  Directive,
  ViewContainerRef,
  TemplateRef,
  OnInit,
  Input,
} from '@angular/core';
import { take } from 'rxjs/operators';
import { User } from '../_models/User';
import { AccountService } from '../_services/account.service';

@Directive({
  selector: '[appHasRole]',
})
export class HasRoleDirective implements OnInit {
  @Input() appHasRole: string[];
  user: User;

  constructor(
    private viewContainerRef: ViewContainerRef,
    private templateRef: TemplateRef<any>,
    private accountService: AccountService
  ) {
    this.accountService.currentUser$.pipe(take(1)).subscribe((user) => {
      this.user = user;
    });
  }

  ngOnInit(): void {
    if (
      !this.user ||
      !this.user?.roles ||
      !this.user?.roles.some((r) => this.appHasRole.includes(r))
    ) {
      this.viewContainerRef.clear();
      return;
    }

    this.viewContainerRef.clear();
  }
}
